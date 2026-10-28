
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using OrderProcessing.Shared.Services;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase2;

public class Phase2Runner
{
    private readonly DataSeeder _dataSeeder;
    private readonly IncomingOrderRepository _incomingOrderRepository;
    private readonly OrderValidator _orderValidator;
    private readonly OrderRepository _orderRepository;
    private readonly ProductRepository _productRepository;
    private readonly RaceConditionDemo _raceConditionDemo;
    private readonly OrderPricer _orderPricer;
    private readonly OrderProcessorV2 _orderProcessor;

    public Phase2Runner(
        DataSeeder dataSeeder,
        IncomingOrderRepository incomingOrderRepository,
        OrderValidator orderValidator,
        OrderRepository orderRepository,
        ProductRepository productRepository,
        RaceConditionDemo raceConditionDemo,
        OrderPricer orderPricer,
        OrderProcessorV2 orderProcessor)
    {
        _dataSeeder = dataSeeder;
        _incomingOrderRepository = incomingOrderRepository;
        _orderValidator = orderValidator;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _raceConditionDemo = raceConditionDemo;
        _orderPricer = orderPricer;
        _orderProcessor = orderProcessor;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        Console.WriteLine("\n========== PHASE 2 ==========\n");

        await _dataSeeder.ResetAsync(ct);

        Console.WriteLine("========== RACE CONDITION DEMO ==========");
        await _raceConditionDemo.RunAsync();

        await _dataSeeder.ResetAsync(ct);

        Console.WriteLine("\n========== PRICING COMPARISON ==========");

        var incomingOrders =
            await _incomingOrderRepository.GetAllAsync(ct);

        if (incomingOrders.Count == 0)
        {
            Log.Error("No incoming orders were found.");
            return;
        }

        var comparisonOrder = incomingOrders[0];

        var timer = Stopwatch.StartNew();

        await _orderPricer.PriceOrderSequentialAsync(comparisonOrder);

        timer.Stop();

        Console.WriteLine(
            $"Sequential pricing: {timer.ElapsedMilliseconds} ms");

        timer.Restart();

        await _orderPricer.PriceOrderAsync(comparisonOrder);

        timer.Stop();

        Console.WriteLine(
            $"Concurrent pricing: {timer.ElapsedMilliseconds} ms");


        Console.WriteLine("\n========== CONCURRENT VALIDATION ==========");

        var validationTasks = new List<Task>();

        foreach (var order in incomingOrders)
        {
            validationTasks.Add(ValidateAndSaveAsync(order, ct));
        }

        foreach (var task in validationTasks)
        {
            await task;
        }

        var validatedOrders =
            await _orderRepository.GetByStatusAsync(
                OrderStatus.Validated,
                ct);

        Console.WriteLine(
            $"Orders ready for processing: {validatedOrders.Count}");

        Console.WriteLine("\n========== CONCURRENT ORDER PROCESSING ==========");

        var processingTimer = Stopwatch.StartNew();
        var processingTasks = new List<Task<bool>>();

        foreach (var order in validatedOrders)
        {
            processingTasks.Add(ProcessOrderAsync(order, ct));
        }

        if (processingTasks.Count > 0)
        {
            Console.WriteLine(
                $"Task[0] status immediately after creation: " +
                $"{processingTasks[0].Status}");
        }

        await Task.Delay(1000, ct);

        Console.WriteLine("\nTask statuses after 1 second:");

        for (int i = 0; i < processingTasks.Count; i++)
        {
            Console.WriteLine(
                $"Order #{validatedOrders[i].Id}: " +
                $"{processingTasks[i].Status}");
        }

        foreach (var task in processingTasks.Select(
                     (task, index) => new
                     {
                         Task = task,
                         Order = validatedOrders[index]
                     }))
        {
            var order = task.Order;

            try
            {
                bool succeeded = await task.Task;

                Console.WriteLine(
                    $"Order #{order.Id} task status: {task.Task.Status}");

                if (!succeeded)
                {
                    Console.WriteLine(
                        $"Order #{order.Id} was rejected.");
                }
            }
            catch (OperationCanceledException)
                when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Order #{order.Id} task status: {task.Task.Status}");

                order.Status = OrderStatus.Failed;
                order.FailureReason = ex.Message;

                // Attempt to release the reserved stock.
                foreach (var item in order.Items)
                {
                    try
                    {
                        await _productRepository.ReleaseStockAsync(
                            item.ProductId,
                            item.Quantity,
                            ct);
                    }
                    catch (Exception releaseEx)
                    {
                        Log.Error(
                            $"Could not release stock for order #{order.Id}, " +
                            $"product #{item.ProductId}: {releaseEx.Message}");
                    }
                }

                await _orderRepository.SaveAsync(order, ct);

                Log.Error(
                    $"Order #{order.Id} failed: {ex.Message}");
            }
        }

        processingTimer.Stop();

        Console.WriteLine("\nFinal task statuses:");

        for (int i = 0; i < processingTasks.Count; i++)
        {
            Console.WriteLine(
                $"Order #{validatedOrders[i].Id}: " +
                $"{processingTasks[i].Status}");
        }

        Console.WriteLine(
            $"Processing duration: {processingTimer.ElapsedMilliseconds} ms");

        // 7. Verify persisted order count and final statuses.
        Console.WriteLine("\n========== FINAL VERIFICATION ==========");

        var savedOrders = await _orderRepository.GetAllAsync(ct);

        int completed = savedOrders.Count(
            order => order.Status == OrderStatus.Completed);

        int rejected = savedOrders.Count(
            order => order.Status == OrderStatus.Rejected);

        int failed = savedOrders.Count(
            order => order.Status == OrderStatus.Failed);

        Console.WriteLine($"Total saved orders: {savedOrders.Count}");
        Console.WriteLine($"Completed: {completed}");
        Console.WriteLine($"Rejected: {rejected}");
        Console.WriteLine($"Failed: {failed}");

        if (savedOrders.Count != 20)
        {
            Log.Error(
                $"Expected 20 orders, but found {savedOrders.Count}.");
        }
        else
        {
            Log.Success(
                "Verified: orders.json contains exactly 20 orders.");
        }
    }

    private async Task ValidateAndSaveAsync(
        Order order,
        CancellationToken ct)
    {
        await _orderValidator.ValidateOrderAsync(
            order,
            ct,
            saveResult: false);

        await _orderRepository.SaveAsync(order, ct);
    }

    private async Task<bool> ProcessOrderAsync(
        Order order,
        CancellationToken ct)
    {
        return await _orderProcessor.ProcessValidatedOrderAsync(
            order,
            ct);
    }
}
