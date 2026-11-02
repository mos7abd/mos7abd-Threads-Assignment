
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Services;

public sealed class LiveCheckoutRunner
{
    private readonly DataSeeder _seeder;
    private readonly OrderProcessor _processor;
    private readonly JsonFileStore _store;

    public LiveCheckoutRunner(
        DataSeeder seeder,
        OrderProcessor processor,
        JsonFileStore store)
    {
        _seeder = seeder;
        _processor = processor;
        _store = store;
    }

    public async Task RunAsync(
        CancellationToken ct = default)
    {
        // Step 1: Reset all data
        await _seeder.ResetAsync(ct);

        // Step 2: Read all incoming orders
        List<Order> orders =
            await _store.ReadAsync<List<Order>>(
                DataFiles.IncomingOrders,
                ct)
            ?? new List<Order>();

        Console.WriteLine(
            $"\nStarting live checkout for {orders.Count} orders...\n");

        // Step 3: Create a channel for completed orders
        Channel<OrderProcessingResult> channel =
            Channel.CreateUnbounded<OrderProcessingResult>();

        // Step 4: Start the consumer before the producers
        Task consumerTask = ConsumeResultsAsync(
            channel.Reader,
            orders.Count,
            ct);

        // Step 5: Start all orders concurrently
        Task[] processingTasks = orders
            .Select(order => ProcessAndPublishAsync(
                order,
                channel.Writer,
                ct))
            .ToArray();

        // Step 6: Wait until all producers finish
        try
        {
            await Task.WhenAll(processingTasks);
        }
        finally
        {
            channel.Writer.TryComplete();
        }

        // Step 7: Wait until all results are printed
        await consumerTask;

        // Step 8: Print final status counts
        await PrintStatusSummaryAsync(ct);
    }

    private async Task ProcessAndPublishAsync(
        Order order,
        ChannelWriter<OrderProcessingResult> writer,
        CancellationToken ct)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            await _processor.ProcessOrderAsync(order, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            order.Status = OrderStatus.Failed;
            order.FailureReason = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
        }

        await writer.WriteAsync(
            new OrderProcessingResult(
                order,
                stopwatch.Elapsed),
            CancellationToken.None);
    }

    private static async Task ConsumeResultsAsync(
        ChannelReader<OrderProcessingResult> reader,
        int totalOrders,
        CancellationToken ct)
    {
        int completedCount = 0;

        await foreach (
            OrderProcessingResult result
            in reader.ReadAllAsync(ct))
        {
            completedCount++;

            Order order = result.Order;

            string reason =
                string.IsNullOrWhiteSpace(order.FailureReason)
                    ? string.Empty
                    : $" ({order.FailureReason})";

            string elapsed = result.Elapsed.TotalSeconds.ToString(
                "F1",
                CultureInfo.InvariantCulture);

            Console.WriteLine(
                $"[{completedCount,2}/{totalOrders}] " +
                $"Order #{order.Id} -> {order.Status}" +
                $"{reason} after {elapsed} s");
        }
    }

    private async Task PrintStatusSummaryAsync(
        CancellationToken ct)
    {
        List<Order> savedOrders =
            await _store.ReadAsync<List<Order>>(
                DataFiles.Orders,
                ct)
            ?? new List<Order>();

        Console.WriteLine("\nFinal status summary:");

        foreach (OrderStatus status in Enum.GetValues<OrderStatus>())
        {
            int count = savedOrders.Count(
                order => order.Status == status);

            Console.WriteLine($"{status}: {count}");
        }

        Console.WriteLine(
            $"Total saved orders: {savedOrders.Count}");
    }
}

