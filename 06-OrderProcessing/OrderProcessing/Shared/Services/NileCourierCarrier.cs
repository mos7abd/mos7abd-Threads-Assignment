
using OrderProcessing.Interfaces;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Utilities;

namespace OrderProcessing.Services
{
    public sealed class NileCourierCarrier : IShippingCarrier
    {
        private readonly JsonFileStore _store;
        private readonly HashSet<int> _failForOrderIds;

        public string Name => "NileCourier";

        public NileCourierCarrier(
            JsonFileStore store,
            IEnumerable<int>? failForOrderIds = null)
        {
            _store = store;
            _failForOrderIds = failForOrderIds?.ToHashSet()
                ?? new HashSet<int>();
        }

        public async Task<decimal> GetQuoteAsync(
            Order order,
            CancellationToken ct)
        {
            try
            {
                Log.Info(
                    $"[{Name}] Starting quote for order #{order.Id}");

                // Step 1: Handshake
                await SimulatedLatency.DelayAsync(
                    $"{Name} handshake",
                    150,
                    ct);

                // Step 2: Check configured failures
                if (_failForOrderIds.Contains(order.Id))
                {
                    throw new ShippingProviderUnavailableException(
                        $"{Name} is unavailable for order #{order.Id}");
                }

                // Step 3: Simulate carrier response time
                int delay =
                    //SimulatedLatency.RandomMilliseconds(1300, 1500);
                    SimulatedLatency.RandomMilliseconds(800, 1200);

                await SimulatedLatency.DelayAsync(
                    $"{Name} response",
                    delay,
                    ct);

                // Step 4: Load products
                var products =
                    await _store.ReadAsync<List<Product>>(
                        DataFiles.Products,
                        ct);

                // Step 5: Calculate total quantity
                decimal totalQuantity = order.Items.Sum(
                    item => item.Quantity);

                // Step 6: Calculate total weight
                decimal totalWeight = order.Items.Sum(item =>
                {
                    var product = products.FirstOrDefault(
                        p => p.Id == item.ProductId);

                    if (product is null)
                    {
                        throw new InvalidOperationException(
                            $"Product #{item.ProductId} not found.");
                    }

                    return product.WeightKg * item.Quantity;
                });

                // Step 7: Calculate base shipping cost
                decimal shippingCost = order.SubTotal >= 1000m
                    ? 0m
                    : 5m
                      + (1.50m * totalQuantity)
                      + (2m * totalWeight);

                // Step 8: Apply NileCourier price factor
                decimal quote = Math.Round(
                    shippingCost * 1.10m,
                    2,
                    MidpointRounding.AwayFromZero);
                ct.ThrowIfCancellationRequested();
                Log.Success(
                    $"[{Name}] Order #{order.Id} quote: {quote}");

                return quote;
            }
            catch (OperationCanceledException)
            {
                Log.Info($"[{Name}] cancelled");
                throw;
            }
        }
    }
}
