# mos7abd-Threads-Assignment
# Order Processing System

## 6.2 JsonFileStore and DataSeeder

### Overview

`JsonFileStore` is responsible for reading and writing JSON data files. Repositories and services should use this class instead of accessing JSON files directly.

`DataSeeder` resets the application's data directory and initializes the JSON files with the required seed data.

### Data Storage

All data files are stored in the `Data` directory under `AppContext.BaseDirectory`.

JSON serialization uses `System.Text.Json` with shared options:

- Indented JSON output.
- Camel-case property names.
- Enum values serialized as strings using `JsonStringEnumConverter`.
- UTF-8 file encoding.

Empty collections are stored as empty JSON arrays (`[]`).

### JsonFileStore API

| Method | Description |
|---|---|
| `ReadAsync<T>(fileName, cancellationToken)` | Reads and deserializes a JSON file asynchronously. |
| `WriteAsync<T>(fileName, data, cancellationToken)` | Serializes and writes data asynchronously. |
| `UpdateAsync<T>(fileName, update, createEmpty, cancellationToken)` | Reads, updates, and writes data while holding a per-file lock. |
| `Read<T>(fileName)` | Reads and deserializes data synchronously. |
| `Write<T>(fileName, data)` | Writes data synchronously. |
| `Update<T>(fileName, update, createEmpty)` | Updates data synchronously while holding a per-file lock. |

The asynchronous methods support cancellation through `CancellationToken`. The synchronous methods are intended for synchronous comparison versions.

### Error Handling and Concurrency

- A missing file requested as `List<T>` returns an empty list.
- Invalid JSON throws `DataFileCorruptedException`, whose message includes the file name.
- Per-file `SemaphoreSlim` locks coordinate concurrent access within the process.
- Writes use a temporary file followed by file replacement to reduce the risk of leaving partially written JSON files.
- `JsonFileStore` does not introduce simulated delays.

### DataSeeder

Call `await DataSeeder.ResetAsync()` at the beginning of each phase.

The reset operation deletes and recreates the `Data` directory, creates the `invoices` and `reports` directories, writes the seed data, initializes the remaining collections as empty arrays, and prints a record-count line for each JSON file.

**Important:** Resetting deletes the existing `Data` directory and its contents.
