
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Utilities;

namespace OrderProcessing.Shared.Repositories;

public class IncomingOrderRepository
{
    private readonly JsonFileStore _store;

    public IncomingOrderRepository(JsonFileStore store)
    {
        _store = store;
    }

    public async Task<List<Order>> GetAllAsync(
        CancellationToken ct = default)
    {
        Log.Info(
            "IncomingOrderRepository -> Opening connection...");

        await SimulatedLatency.DelayAsync(
            "IncomingOrderRepository - Open connection",
            200,
            ct);

        int queryDelay =
            SimulatedLatency.RandomMilliseconds(300, 600);

        Log.Info(
            "IncomingOrderRepository -> Reading incoming orders...");

        await SimulatedLatency.DelayAsync(
            "IncomingOrderRepository - Execute query",
            queryDelay,
            ct);

        List<Order> orders =
            await _store.ReadAsync<List<Order>>(
                DataFiles.IncomingOrders,
                ct)
            ?? new List<Order>();

        Log.Success(
            $"IncomingOrderRepository -> Retrieved {orders.Count} incoming orders.");

        return orders;
    }
}
