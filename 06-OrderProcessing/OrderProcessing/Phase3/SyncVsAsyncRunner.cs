
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Services;

public sealed class SyncVsAsyncRunner
{
    private readonly DataSeeder _seeder;
    private readonly JsonFileStore _store;
    private readonly SyncOrderProcessor _syncProcessor;
    private readonly OrderProcessor _asyncProcessor;

    public SyncVsAsyncRunner(
        DataSeeder seeder,
        JsonFileStore store,
        SyncOrderProcessor syncProcessor,
        OrderProcessor asyncProcessor)
    {
        _seeder = seeder;
        _store = store;
        _syncProcessor = syncProcessor;
        _asyncProcessor = asyncProcessor;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("\n========== SYNC VS ASYNC ==========\n");

        await RunSyncVersionAsync();

        Console.WriteLine("\n-----------------------------------\n");

        await RunAsyncVersionAsync();

        Console.WriteLine("\n========== COMPARISON FINISHED ==========\n");
    }

    private async Task RunSyncVersionAsync()
    {
        Console.WriteLine("========== SYNC VERSION ==========");

        await _seeder.ResetAsync(CancellationToken.None);

        List<Order> orders =
     (await _store.ReadAsync<List<Order>>(
         DataFiles.IncomingOrders,
         CancellationToken.None))
     .Where(o => o.Id >= 1 && o.Id <= 10)
     .ToList();

        var threadIds = new ConcurrentDictionary<int, byte>();
        var threads = new List<Thread>();

        int threadPoolBefore = ThreadPool.ThreadCount;

        Stopwatch stopwatch = Stopwatch.StartNew();

        foreach (Order order in orders)
        {
            var thread = new Thread(() =>
            {
                threadIds.TryAdd(
                    Environment.CurrentManagedThreadId, 0);

                try
                {
                    _syncProcessor.ProcessOrder(order);
                }
                catch (Exception ex)
                {
                    Log.Error(
                        $"Sync Order #{order.Id} failed: {ex.Message}");
                }
            });

            threads.Add(thread);
            thread.Start();
        }

        // Start all threads first, then join them.
        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        stopwatch.Stop();

        int threadPoolAfter = ThreadPool.ThreadCount;

        await PrintResultsAsync(
            "SYNC",
            orders.Count,
            stopwatch.Elapsed,
            threadIds.Count,
            threadPoolBefore,
            threadPoolAfter);
    }

    private async Task RunAsyncVersionAsync()
    {
        Console.WriteLine("========== ASYNC VERSION ==========");

        await _seeder.ResetAsync(CancellationToken.None);

        List<Order> orders =
     (await _store.ReadAsync<List<Order>>(
         DataFiles.IncomingOrders,
         CancellationToken.None))
     .Where(o => o.Id >= 1 && o.Id <= 10)
     .ToList();

        var threadIds = new ConcurrentDictionary<int, byte>();
        var tasks = new List<Task<bool>>();

        int threadPoolBefore = ThreadPool.ThreadCount;

        Stopwatch stopwatch = Stopwatch.StartNew();

        // Start all tasks before awaiting individual tasks.
        foreach (Order order in orders)
        {
            tasks.Add(Task.Run(async () =>
            {
                threadIds.TryAdd(
                    Environment.CurrentManagedThreadId, 0);

                bool result =
                    await _asyncProcessor.ProcessOrderAsync(
                        order,
                        CancellationToken.None);

                threadIds.TryAdd(
                    Environment.CurrentManagedThreadId, 0);

                return result;
            }));
        }

        int completedCount = 0;

        // Await tasks one by one. Do not use Task.WhenAll.
        foreach (Task<bool> task in tasks)
        {
            try
            {
                if (await task)
                {
                    completedCount++;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Async task failed: {ex.Message}");
            }
        }

        stopwatch.Stop();

        int threadPoolAfter = ThreadPool.ThreadCount;

        await PrintResultsAsync(
            "ASYNC",
            orders.Count,
            stopwatch.Elapsed,
            threadIds.Count,
            threadPoolBefore,
            threadPoolAfter);
    }

    private async Task PrintResultsAsync(
        string version,
        int totalOrders,
        TimeSpan elapsed,
        int usedThreads,
        int threadPoolBefore,
        int threadPoolAfter)
    {
        List<Order> savedOrders =
            await _store.ReadAsync<List<Order>>(
                DataFiles.Orders,
                CancellationToken.None);

        int completed = savedOrders.Count(
            o => o.Status == OrderStatus.Completed);

        int rejected = savedOrders.Count(
            o => o.Status == OrderStatus.Rejected);

        Console.WriteLine($"\n========== {version} RESULTS ==========");
        Console.WriteLine($"Total incoming orders : {totalOrders}");
        Console.WriteLine($"Completed             : {completed}");
        Console.WriteLine($"Rejected              : {rejected}");
        Console.WriteLine($"Elapsed time          : {elapsed.TotalMilliseconds:N0} ms");
        Console.WriteLine($"Distinct thread IDs   : {usedThreads}");
        Console.WriteLine($"ThreadPool before     : {threadPoolBefore}");
        Console.WriteLine($"ThreadPool after      : {threadPoolAfter}");
        Console.WriteLine("========================================");
    }
}
