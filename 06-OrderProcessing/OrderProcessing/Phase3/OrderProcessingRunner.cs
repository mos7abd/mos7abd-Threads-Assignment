
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Services;

public sealed class OrderProcessingRunner
{
    private readonly DataSeeder _seeder;
    private readonly JsonFileStore _store;
    private readonly OrderProcessor _processor;

    public OrderProcessingRunner(
        DataSeeder seeder,
        JsonFileStore store,
        OrderProcessor processor)
    {
        _seeder = seeder;
        _store = store;
        _processor = processor;
    }

    public async Task RunAsync(int? cancelAfterMs = null)
    {
        using var runCts = new CancellationTokenSource();
        CancellationToken ct = runCts.Token;

        ConsoleCancelEventHandler cancelHandler = (_, e) =>
        {
            e.Cancel = true;

            Log.Info("Ctrl+C detected. Cancelling the current run...");

            runCts.Cancel();
        };

        Console.CancelKeyPress += cancelHandler;

        try
        {
            if (cancelAfterMs.HasValue)
            {
                if (cancelAfterMs.Value <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(cancelAfterMs),
                        "--cancel-after must be greater than zero.");
                }

                runCts.CancelAfter(cancelAfterMs.Value);

                Log.Info(
                    $"Automatic cancellation scheduled after " +
                    $"{cancelAfterMs.Value} ms.");
            }

            Log.Info("Resetting phase data...");

            await _seeder.ResetAsync(ct);

            ct.ThrowIfCancellationRequested();

            Log.Success("Phase data reset completed.");

            List<Order> incomingOrders =
                await _store.ReadAsync<List<Order>>(
                    DataFiles.IncomingOrders,
                    ct);

            Log.Info(
                $"Loaded {incomingOrders.Count} incoming orders.");

            var tasks = new List<Task<bool>>();

            // Start all orders before awaiting individual tasks.
            foreach (Order order in incomingOrders)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                tasks.Add(_processor.ProcessOrderAsync(order, ct));
            }

            Log.Info(
                $"Started {tasks.Count} order processing tasks.");

            // Await tasks one by one, without Task.WhenAll.
            int completedCount = 0;

            foreach (Task<bool> task in tasks)
            {
                try
                {
                    bool succeeded = await task;

                    if (succeeded)
                    {
                        completedCount++;
                    }
                }
                catch (OperationCanceledException)
                    when (ct.IsCancellationRequested)
                {
                    Log.Info(
                        "Run cancellation observed while awaiting orders.");
                }
                catch (Exception ex)
                {
                    Log.Error(
                        $"An order task failed unexpectedly: {ex.Message}");
                }
            }

            if (ct.IsCancellationRequested)
            {
                Log.Info("Run was cancelled.");
            }
            else
            {
                Log.Success(
                    $"Run completed. Successful orders: {completedCount}.");
            }

            await PrintFinalOrdersAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            Log.Info("Run cancelled before all orders were started.");

            await PrintFinalOrdersAsync(CancellationToken.None);
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    private async Task PrintFinalOrdersAsync(CancellationToken ct)
    {
        List<Order> orders =
            await _store.ReadAsync<List<Order>>(
                DataFiles.Orders,
                ct);

        Console.WriteLine();
        Console.WriteLine(
            "================ FINAL ORDERS ================");

        Console.WriteLine(
            "{0,-10} {1,-15} {2,-12} {3}",
            "Order",
            "Status",
            "Total",
            "Reason");

        Console.WriteLine(
            new string('-', 85));

        foreach (Order order in orders)
        {
            Console.WriteLine(
                "{0,-10} {1,-15} {2,-12:F2} {3}",
                order.Id,
                order.Status,
                order.TotalAmount,
                order.FailureReason ?? "-");
        }

        Console.WriteLine(
            new string('-', 85));

        Console.WriteLine(
            $"Total saved orders: {orders.Count}");

        Console.WriteLine(
            "===============================================");
    }
}
