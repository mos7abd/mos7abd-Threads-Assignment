
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Utilities;

namespace OrderProcessing.Shared.Repositories;

public class OrderRepository
{
    private readonly JsonFileStore _store;
    private readonly HashSet<int> _failSaveForOrderIds;

    public OrderRepository(JsonFileStore store, IEnumerable<int>? failSaveForOrderIds = null)
    {
        _store = store;
        _failSaveForOrderIds = new HashSet<int>(
        failSaveForOrderIds ?? Enumerable.Empty<int>());

    }


    public async Task SaveAsync(
        Order order,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        ct.ThrowIfCancellationRequested();

        // Step 1: Open connection (200 ms)
        Log.Info("OrderRepository -> Opening connection...");

        await SimulatedLatency.DelayAsync(
            "OrderRepository - Open connection",
            200,
            ct);

        // Step 2: Begin transaction (100 ms)
        Log.Info(
            $"OrderRepository -> Beginning transaction for Order #{order.Id}...");

        await SimulatedLatency.DelayAsync(
            "OrderRepository - Begin transaction",
            100,
            ct);

        bool replaced = false;

        // Step 3: Insert / Update (400 ms)
        Log.Info(
            $"OrderRepository -> Saving Order #{order.Id}...");

        await SimulatedLatency.DelayAsync(
            "OrderRepository - Insert/Update",
            400,
            ct);
        if (order.Status == OrderStatus.Completed &&
            _failSaveForOrderIds.Contains(order.Id))
        {
            Log.Error(
                $"OrderRepository -> Failed to save Order #{order.Id}: " +
                "Connection lost during commit");

            throw new DatabaseWriteException(
                "Connection lost during commit");
        }
        await _store.UpdateAsync<List<Order>>(
            DataFiles.Orders,
            orders =>
            {
                int index = orders.FindIndex(
                    existingOrder => existingOrder.Id == order.Id);

                if (index >= 0)
                {
                    orders[index] = order;
                    replaced = true;
                }
                else
                {
                    orders.Add(order);
                }

                return orders;
            },
            () => new List<Order>(),
            ct);

        // Step 4: Commit (200 ms)
        Log.Info(
            $"OrderRepository -> Committing transaction for Order #{order.Id}...");

        await SimulatedLatency.DelayAsync(
            "OrderRepository - Commit",
            200,
            ct);

        if (replaced)
        {
            Log.Success(
                $"OrderRepository -> Order #{order.Id} updated.");
        }
        else
        {
            Log.Success(
                $"OrderRepository -> Order #{order.Id} added.");
        }
    }



    public async Task<List<Order>> GetAllAsync(
        CancellationToken ct = default)
    {
        Log.Info("OrderRepository -> Opening connection...");

        await SimulatedLatency.DelayAsync(
            "OrderRepository - Open connection",
            200,
            ct);

        int queryDelay =
            SimulatedLatency.RandomMilliseconds(300, 600);

        Log.Info(
            "OrderRepository -> Executing query to get all orders...");

        await SimulatedLatency.DelayAsync(
            "OrderRepository - Execute query",
            queryDelay,
            ct);

        List<Order> orders =
            await _store.ReadAsync<List<Order>>(
                DataFiles.Orders,
                ct)
            ?? new List<Order>();

        Log.Success(
            $"OrderRepository -> Retrieved {orders.Count} orders.");

        return orders;
    }


    public async Task<List<Order>> GetByStatusAsync(
        OrderStatus s,
        CancellationToken ct = default)
    {
        Log.Info("OrderRepository -> Opening connection...");

        await SimulatedLatency.DelayAsync(
            "OrderRepository - Open connection",
            200,
            ct);

        int queryDelay =
            SimulatedLatency.RandomMilliseconds(300, 600);

        Log.Info(
            $"OrderRepository -> Executing query for Status '{s}'...");

        await SimulatedLatency.DelayAsync(
            "OrderRepository - Execute query",
            queryDelay,
            ct);

        List<Order> orders =
            await _store.ReadAsync<List<Order>>(
                DataFiles.Orders,
                ct)
            ?? new List<Order>();

        List<Order> filteredOrders = orders
            .Where(order => order.Status == s)
            .ToList();

        Log.Success(
            $"OrderRepository -> Retrieved {filteredOrders.Count} orders with Status '{s}'.");

        return filteredOrders;
    }
    public void Save(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        // Step 1: Open connection (200 ms)
        Log.Info("OrderRepository -> Opening connection...");

        Thread.Sleep(200);

        // Step 2: Begin transaction (100 ms)
        Log.Info(
            $"OrderRepository -> Beginning transaction for Order #{order.Id}...");

        Thread.Sleep(100);

        bool replaced = false;

        // Step 3: Insert / Update (400 ms)
        Log.Info(
            $"OrderRepository -> Saving Order #{order.Id}...");

        Thread.Sleep(400);

        if (order.Status == OrderStatus.Completed &&
            _failSaveForOrderIds.Contains(order.Id))
        {
            Log.Error(
                $"OrderRepository -> Failed to save Order #{order.Id}: " +
                "Connection lost during commit");

            throw new DatabaseWriteException(
                "Connection lost during commit");
        }

        _store.Update<List<Order>>(
            DataFiles.Orders,
            orders =>
            {
                int index = orders.FindIndex(
                    existingOrder => existingOrder.Id == order.Id);

                if (index >= 0)
                {
                    orders[index] = order;
                    replaced = true;
                }
                else
                {
                    orders.Add(order);
                }

                return orders;
            },
            () => new List<Order>());

        // Step 4: Commit (200 ms)
        Log.Info(
            $"OrderRepository -> Committing transaction for Order #{order.Id}...");

        Thread.Sleep(200);

        if (replaced)
        {
            Log.Success(
                $"OrderRepository -> Order #{order.Id} updated.");
        }
        else
        {
            Log.Success(
                $"OrderRepository -> Order #{order.Id} added.");
        }
    }

    public async Task<List<Order>> GetByCustomerIdAsync(
    int customerId,
    CancellationToken ct = default)
    {
        Log.Info(
            $"OrderRepository -> Opening connection for Customer #{customerId}...");

        await SimulatedLatency.DelayAsync(
            "OrderRepository - Open connection",
            200,
            ct);

        int queryDelay =
            SimulatedLatency.RandomMilliseconds(300, 600);

        Log.Info(
            $"OrderRepository -> Executing query for Customer #{customerId}...");

        await SimulatedLatency.DelayAsync(
            "OrderRepository - Execute query",
            queryDelay,
            ct);

        List<Order> allOrders =
            await _store.ReadAsync<List<Order>>(
                DataFiles.Orders,
                ct)
            ?? new List<Order>();

        List<Order> customerOrders = allOrders
            .Where(order => order.CustomerId == customerId)
            .ToList();

        Log.Success(
            $"OrderRepository -> Retrieved {customerOrders.Count} orders " +
            $"for Customer #{customerId}.");

        return customerOrders;
    }

}
