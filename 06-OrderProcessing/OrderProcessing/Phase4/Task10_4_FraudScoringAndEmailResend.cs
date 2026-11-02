
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Services;
using OrderProcessing.Shared.Storage;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace OrderProcessing.Phase4;

public class Task10_4_FraudScoringAndEmailResend
{
    public void RunFraudScoring(CancellationToken ct = default)
    {
        // Create 10,000 orders in memory
        var orders = Enumerable.Range(1, 10_000)
            .Select(id => new Order { Id = id })
            .ToList();

        // Sequential Fraud Scoring
        var sequentialStopwatch = Stopwatch.StartNew();

        var sequentialFlaggedOrders = new List<int>();

        foreach (var order in orders)
        {
            ct.ThrowIfCancellationRequested();

            if (Score(order.Id) >= 90)
            {
                sequentialFlaggedOrders.Add(order.Id);
            }
        }

        sequentialStopwatch.Stop();

        // Parallel Fraud Scoring
        var parallelResult = RunParallelFraudScoring(orders, ct);

        // Compare Results
        var sequentialIds = sequentialFlaggedOrders
            .OrderBy(id => id)
            .ToList();

        var parallelIds = parallelResult.FlaggedOrders
            .OrderBy(id => id)
            .ToList();

        bool resultsMatch = sequentialIds.SequenceEqual(parallelIds);

        // Calculate Speed-up
        double speedUp = parallelResult.ElapsedMilliseconds > 0
            ? (double)sequentialStopwatch.ElapsedMilliseconds /
              parallelResult.ElapsedMilliseconds
            : 0;

        // Display Results
        Console.WriteLine("\n========== Fraud Scoring Results ==========");

        Console.WriteLine($"Sequential Flagged Count: {sequentialFlaggedOrders.Count}");
        Console.WriteLine($"Sequential Time: {sequentialStopwatch.ElapsedMilliseconds} ms");

        Console.WriteLine($"Parallel Flagged Count: {parallelResult.FlaggedOrders.Count}");
        Console.WriteLine($"Parallel Time: {parallelResult.ElapsedMilliseconds} ms");

        Console.WriteLine($"Speed-up: {speedUp:F2}x");

        Console.WriteLine($"Distinct Threads Used: {parallelResult.DistinctThreadsUsed}");
        Console.WriteLine($"Processor Count: {Environment.ProcessorCount}");

        Console.WriteLine($"Results Match: {resultsMatch}");

        Console.WriteLine($"Expected Flagged Count (1005): {sequentialFlaggedOrders.Count == 1005}");

        Console.WriteLine(
            $"First Five Flagged IDs: {string.Join(", ", sequentialIds.Take(5))}");

        Console.WriteLine("============================================");
    }

    private (List<int> FlaggedOrders, long ElapsedMilliseconds, int DistinctThreadsUsed)
        RunParallelFraudScoring(List<Order> orders, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        var flaggedOrders = new ConcurrentBag<int>();

        var usedThreads = new ConcurrentDictionary<int, byte>();

        Parallel.ForEach(
            orders,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = ct
            },
            order =>
            {
                usedThreads.TryAdd(
                    Environment.CurrentManagedThreadId, 0);

                if (Score(order.Id) >= 90)
                {
                    flaggedOrders.Add(order.Id);
                }
            });

        stopwatch.Stop();

        return (
            flaggedOrders.ToList(),
            stopwatch.ElapsedMilliseconds,
            usedThreads.Count
        );
    }

    private static int Score(int orderId)
    {
        long h = orderId;

        for (int i = 0; i < 100_000; i++)
            h = (h * 31 + i) % 1_000_000_007;

        return (int)(h % 100);
    }


    public async Task RunEmailResendAsync(CancellationToken ct = default)
    {
        var store = new JsonFileStore();

        var emailService = new EmailService(
            store,
            failuresEnabled: false);

        // Create 30 test orders and customers (1001–1030)
        var emailJobs = Enumerable.Range(1001, 30)
            .Select(id =>
            {
                var order = new Order
                {
                    Id = id,
                    CustomerId = id
                };

                var customer = new Customer
                {
                    Id = id,
                    Name = $"Customer {id}",
                    Email = $"customer{id}@example.com",
                    IsActive = true
                };

                return (Order: order, Customer: customer);
            })
            .ToList();

        int currentSends = 0;
        int maxConcurrentSends = 0;

        var stopwatch = Stopwatch.StartNew();

        await Parallel.ForEachAsync(
            emailJobs,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = 4,
                CancellationToken = ct
            },
            async (job, token) =>
            {
                int current = Interlocked.Increment(ref currentSends);

                // Observe maximum concurrency without controlling it.
                int previousMax;

                do
                {
                    previousMax = Volatile.Read(ref maxConcurrentSends);

                    if (current <= previousMax)
                        break;

                } while (Interlocked.CompareExchange(
                    ref maxConcurrentSends,
                    current,
                    previousMax) != previousMax);

                try
                {
                    await emailService.SendConfirmationAsync(
                        job.Order,
                        job.Customer,
                        token);
                }
                finally
                {
                    Interlocked.Decrement(ref currentSends);
                }
            });

        stopwatch.Stop();

        Console.WriteLine("\n========== Email Re-send Results ==========");
        Console.WriteLine($"Total Emails: {emailJobs.Count}");
        Console.WriteLine($"Max Concurrent Sends: {maxConcurrentSends}");
        Console.WriteLine($"Total Execution Time: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine("============================================");
    }

}
