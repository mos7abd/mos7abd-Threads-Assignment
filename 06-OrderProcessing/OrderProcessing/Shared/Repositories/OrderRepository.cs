
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Utilities;

namespace OrderProcessing.Shared.Repositories;

public class OrderRepository
{
    private readonly JsonFileStore _store;

    public OrderRepository(JsonFileStore store)
    {
        _store = store;
    }

    public async Task SaveAsync(
        Order order,
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
            $"OrderRepository -> Saving Order #{order.Id}...");

        await SimulatedLatency.DelayAsync(
            "OrderRepository - Execute query",
            queryDelay,
            ct);

        bool replaced = false;

        await _store.UpdateAsync<List<Order>>(
            DataFiles.Orders,
            orders =>
            {
                int index = orders.FindIndex(
                    o => o.Id == order.Id);

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


}
