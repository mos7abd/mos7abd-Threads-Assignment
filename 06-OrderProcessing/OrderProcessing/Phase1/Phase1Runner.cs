
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using OrderProcessing.Shared.Services;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1;

public class Phase1Runner
{
    private readonly DataSeeder _dataSeeder;
    private readonly IncomingOrderRepository _incomingOrderRepository;
    private readonly SyncOrderValidator _syncOrderValidator;
    private readonly OrderValidator _orderValidator;
    private readonly OrderRepository _orderRepository;

    public Phase1Runner(
        DataSeeder dataSeeder,
        IncomingOrderRepository incomingOrderRepository,
        SyncOrderValidator syncOrderValidator,
        OrderValidator orderValidator,
        OrderRepository orderRepository)
    {
        _dataSeeder = dataSeeder;
        _incomingOrderRepository = incomingOrderRepository;
        _syncOrderValidator = syncOrderValidator;
        _orderValidator = orderValidator;
        _orderRepository = orderRepository;
    }

    public async Task RunAsync(
        CancellationToken ct = default)
    {
        // Reset data
        await _dataSeeder.ResetAsync(ct);

        // Load incoming orders
        var orders =
            await _incomingOrderRepository.GetAllAsync(ct);

        // Sync run: validate without saving
        Stopwatch syncTimer = Stopwatch.StartNew();

        foreach (var order in orders)
        {
            ct.ThrowIfCancellationRequested();
            _syncOrderValidator.ValidateOrder(order);
        }

        syncTimer.Stop();

        // Reset data before async run
        await _dataSeeder.ResetAsync(ct);

        // Async run: validate, then save each order
        Stopwatch asyncTimer = Stopwatch.StartNew();

        foreach (var order in orders)
        {
            ct.ThrowIfCancellationRequested();

            await _orderValidator.ValidateOrderAsync(
                order,
                ct,
                saveResult: false);

            await _orderRepository.SaveAsync(order, ct);
        }

        asyncTimer.Stop();

        // Read counts from orders.json
        var validatedOrders =
            await _orderRepository.GetByStatusAsync(
                OrderStatus.Validated, ct);

        var rejectedOrders =
            await _orderRepository.GetByStatusAsync(
                OrderStatus.Rejected, ct);

        var savedOrders =
            await _orderRepository.GetAllAsync(ct);

        // Print summary
        Console.WriteLine();
        Console.WriteLine("===== Validation Summary =====");
        Console.WriteLine($"Total Orders: {savedOrders.Count}");
        Console.WriteLine($"Validated:    {validatedOrders.Count}");
        Console.WriteLine($"Rejected:     {rejectedOrders.Count}");

        foreach (var order in rejectedOrders.OrderBy(o => o.Id))
        {
            Console.WriteLine(
                $"  - Order #{order.Id}:  {order.FailureReason}");
        }

        Console.WriteLine(
            $"Sync Time:  {syncTimer.Elapsed.TotalSeconds:F1} seconds");

        Console.WriteLine(
            $"Async Time: {asyncTimer.Elapsed.TotalSeconds:F1} seconds");

        Console.WriteLine(
            $"orders.json now contains {savedOrders.Count} records.");
    }
}
