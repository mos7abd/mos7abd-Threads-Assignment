
using OrderProcessing.Interfaces;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderProcessing.Services
{
    public class FastShipCarrier : IShippingCarrier
    {
        private readonly JsonFileStore _store;
        private readonly HashSet<int> _failForOrderIds;

        public string Name => "FastShip";

        public FastShipCarrier(
            JsonFileStore store,
            IEnumerable<int>? failForOrderIds = null)
        {
            _store = store;
            _failForOrderIds = new HashSet<int>(
                failForOrderIds ?? Array.Empty<int>());
        }

        public async Task<decimal> GetQuoteAsync(
            Order order,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(order);

            try
            {
                Log.Info(
                    $"[{Name}] Requesting quote for Order #{order.Id}...");

                // Step 1: Handshake (150 ms).
                await SimulatedLatency.DelayAsync(
                    $"{Name} - Handshake",
                    150,
                    ct);

                // Step 2: Configured failure.
                if (_failForOrderIds.Contains(order.Id))
                {
                    throw new ShippingProviderUnavailableException(
                        $"{Name} unavailable for Order #{order.Id}");
                }

                // Step 3: Random response time (500–1500 ms).
                int responseDelay =
                   SimulatedLatency.RandomMilliseconds(500, 1500);

                await SimulatedLatency.DelayAsync(
                    $"{Name} - Get quote",
                    responseDelay,
                    ct);

                // Step 4: Load products.
                List<Product> products =
                    await _store.ReadAsync<List<Product>>(
                        DataFiles.Products,
                        ct)
                    ?? throw new InvalidOperationException(
                        "Products file could not be loaded.");

                decimal totalQuantity = 0m;
                decimal totalWeight = 0m;

                // Step 5: Calculate quantity and total weight.
                foreach (var item in order.Items)
                {
                    ct.ThrowIfCancellationRequested();

                    Product? product =
                        products.Find(p => p.Id == item.ProductId);

                    if (product is null)
                    {
                        throw new InvalidOperationException(
                            $"Product #{item.ProductId} was not found.");
                    }

                    totalQuantity += item.Quantity;
                    totalWeight += product.WeightKg * item.Quantity;
                }

                // Step 6: Calculate base shipping price.
                decimal basePrice = order.SubTotal >= 1000m
                    ? 0m
                    : 5m
                      + (1.50m * totalQuantity)
                      + (2m * totalWeight);

                // Step 7: Apply FastShip price factor.
                decimal quote = Math.Round(
                    basePrice * 1.00m,
                    2,
                    MidpointRounding.AwayFromZero);

                // Do not return a quote if cancellation was requested.
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
