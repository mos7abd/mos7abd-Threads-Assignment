
using OrderProcessing.Interfaces;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderProcessing.Services
{
    public sealed class ShippingQuoteService
    {
        private readonly IReadOnlyList<IShippingCarrier> _carriers;

        public ShippingQuoteService(
            IEnumerable<IShippingCarrier> carriers)
        {
            _carriers = carriers.ToList();

            if (_carriers.Count != 3)
            {
                throw new ArgumentException(
                    "Exactly three shipping carriers are required.");
            }
        }

        public async Task<ShippingQuote> GetFastestQuoteAsync(
            Order order,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(order);

            const int timeBudgetMs = 1200;
            const decimal flatRate = 25.00m;

            var stopwatch = Stopwatch.StartNew();

            using var cts =
                CancellationTokenSource.CreateLinkedTokenSource(ct);

            var pendingTasks = new Dictionary<
                Task<decimal>,
                IShippingCarrier>();

            // R1: Start all three carriers.
            foreach (var carrier in _carriers)
            {
                ct.ThrowIfCancellationRequested();

                Task<decimal> task =
                    carrier.GetQuoteAsync(order, cts.Token);

                pendingTasks.Add(task, carrier);
            }

            var timeoutTask = Task.Delay(timeBudgetMs, ct);

            try
            {
                // R2: Wait for the first successful quote.
                while (pendingTasks.Count > 0)
                {
                    Task completedTask = await Task.WhenAny(
                        pendingTasks.Keys.Cast<Task>()
                            .Append(timeoutTask));

                    // R7: Respect caller cancellation.
                    ct.ThrowIfCancellationRequested();

                    // R4: Time budget expired.
                    if (completedTask == timeoutTask)
                    {
                        break;
                    }

                    var completedQuoteTask =
                        (Task<decimal>)completedTask;

                    IShippingCarrier completedCarrier =
                        pendingTasks[completedQuoteTask];

                    pendingTasks.Remove(completedQuoteTask);

                    try
                    {
                        decimal price = await completedQuoteTask;

                        // R3: A successful quote wins.
                        var cancelledCarriers = pendingTasks.Values
                            .Select(carrier => carrier.Name)
                            .ToList();

                        stopwatch.Stop();

                        cts.Cancel();

                        // Wait for cancelled carriers to finish.
                        await ObserveRemainingTasksAsync(
                            pendingTasks.Keys);

                        return new ShippingQuote
                        {
                            Carrier = completedCarrier.Name,
                            Price = price,
                            ElapsedMs = stopwatch.ElapsedMilliseconds,
                            CancelledCarriers = cancelledCarriers
                        };
                    }
                    catch (ShippingProviderUnavailableException ex)
                    {
                        Log.Info(
                            $"[{completedCarrier.Name}] failed: {ex.Message}");
                    }
                }

                // R6: All carriers failed before the timeout.
                if (pendingTasks.Count == 0)
                {
                    throw new ShippingProviderUnavailableException(
                        "All carriers failed");
                }

                // R4: No successful quote arrived within 1.2 seconds.
                var timeoutCancelledCarriers = pendingTasks.Values
                    .Select(carrier => carrier.Name)
                    .ToList();

                cts.Cancel();

                stopwatch.Stop();

                // Wait for all cancelled tasks to finish.
                await ObserveRemainingTasksAsync(
                    pendingTasks.Keys);

                return new ShippingQuote
                {
                    Carrier = "Flat rate",
                    Price = flatRate,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    CancelledCarriers = timeoutCancelledCarriers
                };
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // R7: Caller requested cancellation.
                cts.Cancel();

                await ObserveRemainingTasksAsync(pendingTasks.Keys);

                throw;
            }
            finally
            {
                cts.Cancel();
            }
        }

        // Observe all remaining tasks after cancellation.
        private static async Task ObserveRemainingTasksAsync(
            IEnumerable<Task<decimal>> tasks)
        {
            var observers = tasks.Select(async task =>
            {
                try
                {
                    await task;
                }
                catch (OperationCanceledException)
                {
                    // Expected: the carrier was cancelled.
                }
                catch (ShippingProviderUnavailableException)
                {
                    // Expected: the carrier failed.
                }
            });

            await Task.WhenAll(observers);
        }
    }
}
