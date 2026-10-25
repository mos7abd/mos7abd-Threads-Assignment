# Assignment 01 — Answers

Student name: Mostafa Abdellah  
Group / ID: Group 4

## Problems with the current design

- The orders were processed sequentially. Each order had to finish processing before the next order started, increasing the total execution time.
- `Thread.Sleep()` blocked the current thread during simulated I/O operations.
- The customer profiles were read using synchronous file operations, which blocked the current thread.
- Invoice files were written using synchronous file operations, preventing the application from continuing while waiting for each write operation.
- The design did not allow independent order-processing operations to overlap.

## What you changed

- Replaced `Thread.Sleep()` with `Task.Delay()` in the database methods to avoid blocking the current thread during simulated delays.
- Converted `GetPendingOrders()` and `GetOrderById()` to asynchronous methods.
- Replaced `File.ReadAllText()` with `File.ReadAllTextAsync()` to read customer profiles asynchronously.
- Replaced `File.WriteAllText()` with `File.WriteAllTextAsync()` to write invoice files asynchronously.
- Updated `InvoiceExportService` to start processing all orders before awaiting each task individually. This allows independent order-processing operations to overlap.
- Updated `Program.cs` to await `ExportAllPendingAsync()`.
- Preserved the original business flow: load order → read