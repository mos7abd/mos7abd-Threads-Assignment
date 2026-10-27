using OrderProcessing.Shared.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace OrderProcessing.Shared.Storage
{
    public class JsonFileStore
    {
        private static readonly string DataDirectory =
            Path.Combine(AppContext.BaseDirectory, "Data");

        private static readonly JsonSerializerOptions JsonOptions =
            CreateJsonOptions();

        private static readonly ConcurrentDictionary<string, SemaphoreSlim>
            FileLocks = new();

        private static JsonSerializerOptions CreateJsonOptions()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            options.Converters.Add(new JsonStringEnumConverter());

            return options;
        }

        private static SemaphoreSlim GetFileLock(string filePath)
        {
            return FileLocks.GetOrAdd(
                filePath,
                _ => new SemaphoreSlim(1, 1));
        }

        private static string GetFilePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) ||
                Path.IsPathRooted(fileName) ||
                fileName == "." ||
                fileName == ".." ||
                !string.Equals(
                    Path.GetFileName(fileName),
                    fileName,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "A valid data file name is required.",
                    nameof(fileName));
            }

            return Path.GetFullPath(
                Path.Combine(DataDirectory, fileName));
        }

        private async Task<T?> ReadFileCoreAsync<T>(
            string fileName,
            string filePath,
            CancellationToken cancellationToken)
        {
            if (!File.Exists(filePath))
            {
                if (typeof(T).IsGenericType &&
                    typeof(T).GetGenericTypeDefinition() == typeof(List<>))
                {
                    return (T)Activator.CreateInstance(typeof(T))!;
                }

                return default;
            }

            string json = await File.ReadAllTextAsync(
                filePath,
                Encoding.UTF8,
                cancellationToken);

            try
            {
                T? result = JsonSerializer.Deserialize<T>(
                    json,
                    JsonOptions);

                if (result is null)
                {
                    throw new JsonException(
                        "The JSON content produced a null result.");
                }

                return result;
            }
            catch (JsonException ex)
            {
                throw new DataFileCorruptedException(
                    $"File '{fileName}' contains invalid JSON.",
                    ex);
            }
        }

        private async Task WriteFileCoreAsync<T>(
            string filePath,
            T data,
            CancellationToken cancellationToken)
        {
            string tempFilePath = filePath + ".tmp";

            try
            {
                string json = JsonSerializer.Serialize(
                    data,
                    JsonOptions);

                await File.WriteAllTextAsync(
                    tempFilePath,
                    json,
                    Encoding.UTF8,
                    cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                File.Move(
                    tempFilePath,
                    filePath,
                    overwrite: true);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        public async Task<T?> ReadAsync<T>(
            string fileName,
            CancellationToken cancellationToken = default)
        {
            string filePath = GetFilePath(fileName);

            cancellationToken.ThrowIfCancellationRequested();

            SemaphoreSlim fileLock = GetFileLock(filePath);

            await fileLock.WaitAsync(cancellationToken);

            try
            {
                return await ReadFileCoreAsync<T>(
                    fileName,
                    filePath,
                    cancellationToken);
            }
            finally
            {
                fileLock.Release();
            }
        }

        public async Task WriteAsync<T>(
            string fileName,
            T data,
            CancellationToken cancellationToken = default)
        {
            string filePath = GetFilePath(fileName);

            Directory.CreateDirectory(
                Path.GetDirectoryName(filePath)!);

            SemaphoreSlim fileLock = GetFileLock(filePath);

            await fileLock.WaitAsync(cancellationToken);

            try
            {
                await WriteFileCoreAsync(
                    filePath,
                    data,
                    cancellationToken);
            }
            finally
            {
                fileLock.Release();
            }
        }

        public async Task<T> UpdateAsync<T>(
            string fileName,
            Func<T, T> update,
            Func<T> createEmpty,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(update);
            ArgumentNullException.ThrowIfNull(createEmpty);

            string filePath = GetFilePath(fileName);

            Directory.CreateDirectory(
                Path.GetDirectoryName(filePath)!);

            SemaphoreSlim fileLock = GetFileLock(filePath);

            await fileLock.WaitAsync(cancellationToken);

            try
            {
                T? current = await ReadFileCoreAsync<T>(
                    fileName,
                    filePath,
                    cancellationToken);

                T currentData = current is null
                    ? createEmpty()
                    : current;

                T updatedData = update(currentData);

                cancellationToken.ThrowIfCancellationRequested();

                await WriteFileCoreAsync(
                    filePath,
                    updatedData,
                    cancellationToken);

                return updatedData;
            }
            finally
            {
                fileLock.Release();
            }
        }

        public T? Read<T>(string fileName)
        {
            string filePath = GetFilePath(fileName);

            SemaphoreSlim fileLock = GetFileLock(filePath);

            fileLock.Wait();

            try
            {
                if (!File.Exists(filePath))
                {
                    if (typeof(T).IsGenericType &&
                        typeof(T).GetGenericTypeDefinition() == typeof(List<>))
                    {
                        return (T)Activator.CreateInstance(typeof(T))!;
                    }

                    return default;
                }

                string json = File.ReadAllText(
                    filePath,
                    Encoding.UTF8);

                try
                {
                    T? result = JsonSerializer.Deserialize<T>(
                        json,
                        JsonOptions);

                    if (result is null)
                    {
                        throw new JsonException(
                            "The JSON content produced a null result.");
                    }

                    return result;
                }
                catch (JsonException ex)
                {
                    throw new DataFileCorruptedException(
                        $"File '{fileName}' contains invalid JSON.",
                        ex);
                }
            }
            finally
            {
                fileLock.Release();
            }
        }

        public void Write<T>(
            string fileName,
            T data)
         {
            string filePath = GetFilePath(fileName);

            Directory.CreateDirectory(
                Path.GetDirectoryName(filePath)!);

            SemaphoreSlim fileLock = GetFileLock(filePath);

            fileLock.Wait();

            try
            {
                string tempFilePath = filePath + ".tmp";

                try
                {
                    string json = JsonSerializer.Serialize(
                        data,
                        JsonOptions);

                    File.WriteAllText(
                        tempFilePath,
                        json,
                        Encoding.UTF8);

                    File.Move(
                        tempFilePath,
                        filePath,
                        overwrite: true);
                }
                finally
                {
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }
            }
            finally
            {
                fileLock.Release();
            }
        }

        public T Update<T>(
            string fileName,
            Func<T, T> update,
            Func<T> createEmpty)
        {
            ArgumentNullException.ThrowIfNull(update);
            ArgumentNullException.ThrowIfNull(createEmpty);

            string filePath = GetFilePath(fileName);

            Directory.CreateDirectory(
                Path.GetDirectoryName(filePath)!);

            SemaphoreSlim fileLock = GetFileLock(filePath);

            fileLock.Wait();

            try
            {
                T? current;

                if (!File.Exists(filePath))
                {
                    current = default;
                }
                else
                {
                    string json = File.ReadAllText(
                        filePath,
                        Encoding.UTF8);

                    try
                    {
                        current = JsonSerializer.Deserialize<T>(
                            json,
                            JsonOptions);

                        if (current is null)
                        {
                            throw new JsonException(
                                "The JSON content produced a null result.");
                        }
                    }
                    catch (JsonException ex)
                    {
                        throw new DataFileCorruptedException(
                            $"File '{fileName}' contains invalid JSON.",
                            ex);
                    }
                }

                T currentData = current is null
                    ? createEmpty()
                    : current;

                T updatedData = update(currentData);

                string tempFilePath = filePath + ".tmp";

                try
                {
                    string updatedJson = JsonSerializer.Serialize(
                        updatedData,
                        JsonOptions);

                    File.WriteAllText(
                        tempFilePath,
                        updatedJson,
                        Encoding.UTF8);

                    File.Move(
                        tempFilePath,
                        filePath,
                        overwrite: true);
                }
                finally
                {
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }

                return updatedData;
            }
            finally
            {
                fileLock.Release();
            }
        }


    }
}
