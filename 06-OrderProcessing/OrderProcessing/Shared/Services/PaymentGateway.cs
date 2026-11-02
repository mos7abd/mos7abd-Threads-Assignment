
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Interfaces;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Utilities;

namespace OrderProcessing.Services;

public sealed class PaymentGateway : IPaymentGateway
{
    private const string PaymentsFile = "payments.json";

    private readonly JsonFileStore _store;

    private readonly HashSet<int> _timeoutOrderIds;
    private readonly HashSet<int> _declineOrderIds;

    public PaymentGateway(
    JsonFileStore store,
    IEnumerable<int>? timeoutOrderIds = null,
    IEnumerable<int>? declineOrderIds = null,
    bool failuresEnabled = true)
    {
        _store = store;

        _timeoutOrderIds = failuresEnabled
            ? new HashSet<int>(timeoutOrderIds ?? Enumerable.Empty<int>())
            : new HashSet<int>();

        _declineOrderIds = failuresEnabled
            ? new HashSet<int>(declineOrderIds ?? Enumerable.Empty<int>())
            : new HashSet<int>();
    }

    public async Task<PaymentResult> ProcessPaymentAsync(
        int orderId,
        decimal amount,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        Log.Info(
            $"PaymentGateway -> Starting payment for Order #{orderId}...");

        try
        {
            // Step 1: Authorize
            await SimulatedLatency.DelayAsync(
                $"PaymentGateway - Authorizing Order #{orderId}",
                800,
                ct);

            // Step 2: Fraud check
            await SimulatedLatency.DelayAsync(
                $"PaymentGateway - Fraud check for Order #{orderId}",
                500,
                ct);
            // Check whether this order should be declined
            if (_declineOrderIds.Contains(orderId))
            {
                Log.Info(
                    $"PaymentGateway -> Payment declined for Order #{orderId}: " +
                    "Card declined by issuer.");

                // Create a declined payment transaction.
                var declinedTransaction = new PaymentTransaction
                {
                    OrderId = orderId,
                    Amount = amount,
                    Status = PaymentStatus.Declined,
                    TransactionId = null,
                    CreatedAt = DateTime.UtcNow
                };

                // Save declined transaction to payments.json.
                await _store.UpdateAsync<List<PaymentTransaction>>(
                    PaymentsFile,
                    transactions =>
                    {
                        transactions.Add(declinedTransaction);
                        return transactions;
                    },
                    () => new List<PaymentTransaction>(),
                    ct);

                Log.Success(
                    $"PaymentGateway -> Declined transaction saved for Order #{orderId}.");

                return new PaymentResult
                {
                    Success = false,
                    TransactionId = string.Empty,
                    Status = PaymentStatus.Declined,
                    Message = "Card declined by issuer"
                };
            }
            // Step 3: Capture
            int captureDelay = _timeoutOrderIds.Contains(orderId)
                ? 5000
                : 700;

            await SimulatedLatency.DelayAsync(
                $"PaymentGateway - Capturing Order #{orderId}",
                captureDelay,
                ct);

            ct.ThrowIfCancellationRequested();

            string transactionId =
                "TXN-" +
                Guid.NewGuid().ToString("N")[..12].ToUpper();

            var transaction = new PaymentTransaction
            {
                OrderId = orderId,
                Amount = amount,
                Status = PaymentStatus.Captured,
                TransactionId = transactionId,
                CreatedAt = DateTime.UtcNow
            };

            Log.Info(
                "PaymentGateway -> Saving transaction to payments.json...");

            await _store.UpdateAsync<List<PaymentTransaction>>(
                PaymentsFile,
                transactions =>
                {
                    transactions.Add(transaction);
                    return transactions;
                },
                () => new List<PaymentTransaction>(),
                ct);

            Log.Success(
                $"PaymentGateway -> Payment captured for Order #{orderId}. " +
                $"TransactionId: {transactionId}");

            return new PaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                Status = PaymentStatus.Captured,
                Message = "Payment captured successfully."
            };
        }
        catch (OperationCanceledException)
        {
            Log.Info(
                $"PaymentGateway -> Payment cancelled for Order #{orderId}");

            throw;
        }
        catch (Exception ex)
        {
            Log.Error(
                $"PaymentGateway -> Payment failed for Order #{orderId}: {ex.Message}");

            throw;
        }
    }

    public async Task RefundAsync(
        string transactionId,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);
        ct.ThrowIfCancellationRequested();

        Log.Info(
            $"PaymentGateway -> Starting refund for transaction {transactionId}...");

        try
        {
            // Step 1: Simulate external API connection
            await SimulatedLatency.DelayAsync(
                "PaymentGateway - External API connection",
                150,
                ct);

            // Step 2: Simulate sending the refund request
            await SimulatedLatency.DelayAsync(
                "PaymentGateway - Sending refund request",
                300,
                ct);

            // Step 3: Simulate waiting for the API response
            int responseDelay =
                SimulatedLatency.RandomMilliseconds(500, 1500);

            await SimulatedLatency.DelayAsync(
                "PaymentGateway - Waiting for refund response",
                responseDelay,
                ct);

            ct.ThrowIfCancellationRequested();

            // Update the existing transaction
            await _store.UpdateAsync<List<PaymentTransaction>>(
                PaymentsFile,
                transactions =>
                {
                    var transaction = transactions.Find(
                        t => string.Equals(
                            t.TransactionId,
                            transactionId,
                            StringComparison.OrdinalIgnoreCase));

                    if (transaction is null)
                    {
                        throw new InvalidOperationException(
                            $"Transaction '{transactionId}' was not found.");
                    }

                    if (transaction.Status != PaymentStatus.Captured)
                    {
                        throw new InvalidOperationException(
                            $"Transaction '{transactionId}' cannot be refunded " +
                            $"because its status is {transaction.Status}.");
                    }

                    transaction.Status = PaymentStatus.Refunded;

                    return transactions;
                },
                () => new List<PaymentTransaction>(),
                ct);

            Log.Success(
                $"PaymentGateway -> Transaction {transactionId} refunded successfully.");
        }
        catch (OperationCanceledException)
        {
            Log.Info(
                $"PaymentGateway -> Refund cancelled for transaction {transactionId}");

            throw;
        }
        catch (Exception ex)
        {
            Log.Error(
                $"PaymentGateway -> Refund failed for transaction {transactionId}: {ex.Message}");

            throw;
        }
    }
    public PaymentResult ProcessPayment(int orderId, decimal amount)
    {
        Log.Info(
            $"PaymentGateway -> Starting payment for Order #{orderId}...");

        try
        {
            // Step 1: Authorize (800 ms)
            Thread.Sleep(800);

            // Step 2: Fraud check (500 ms)
            Thread.Sleep(500);

            // Check whether this order should be declined
            if (_declineOrderIds.Contains(orderId))
            {
                Log.Info(
                    $"PaymentGateway -> Payment declined for Order #{orderId}: " +
                    "Card declined by issuer.");

                var declinedTransaction = new PaymentTransaction
                {
                    OrderId = orderId,
                    Amount = amount,
                    Status = PaymentStatus.Declined,
                    TransactionId = null,
                    CreatedAt = DateTime.UtcNow
                };

                _store.Update<List<PaymentTransaction>>(
                    PaymentsFile,
                    transactions =>
                    {
                        transactions.Add(declinedTransaction);
                        return transactions;
                    },
                    () => new List<PaymentTransaction>());

                Log.Success(
                    $"PaymentGateway -> Declined transaction saved for Order #{orderId}.");

                return new PaymentResult
                {
                    Success = false,
                    TransactionId = string.Empty,
                    Status = PaymentStatus.Declined,
                    Message = "Card declined by issuer"
                };
            }

            // Step 3: Capture
            int captureDelay = _timeoutOrderIds.Contains(orderId)
                ? 5000
                : 700;

            Thread.Sleep(captureDelay);

            string transactionId =
                "TXN-" +
                Guid.NewGuid().ToString("N")[..12].ToUpper();

            var transaction = new PaymentTransaction
            {
                OrderId = orderId,
                Amount = amount,
                Status = PaymentStatus.Captured,
                TransactionId = transactionId,
                CreatedAt = DateTime.UtcNow
            };

            Log.Info(
                "PaymentGateway -> Saving transaction to payments.json...");

            _store.Update<List<PaymentTransaction>>(
                PaymentsFile,
                transactions =>
                {
                    transactions.Add(transaction);
                    return transactions;
                },
                () => new List<PaymentTransaction>());

            Log.Success(
                $"PaymentGateway -> Payment captured for Order #{orderId}. " +
                $"TransactionId: {transactionId}");

            return new PaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                Status = PaymentStatus.Captured,
                Message = "Payment captured successfully."
            };
        }
        catch (Exception ex)
        {
            Log.Error(
                $"PaymentGateway -> Payment failed for Order #{orderId}: {ex.Message}");

            throw;
        }
    }
    public void Refund(string transactionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);

        Log.Info(
            $"PaymentGateway -> Starting refund for transaction {transactionId}...");

        try
        {
            // Step 1: External API connection (150 ms)
            Thread.Sleep(150);

            // Step 2: Sending refund request (300 ms)
            Thread.Sleep(300);

            // Step 3: Waiting for API response (500–1500 ms)
            int responseDelay =
                SimulatedLatency.RandomMilliseconds(500, 1500);

            Thread.Sleep(responseDelay);

            // Update the existing transaction
            _store.Update<List<PaymentTransaction>>(
                PaymentsFile,
                transactions =>
                {
                    var transaction = transactions.Find(
                        t => string.Equals(
                            t.TransactionId,
                            transactionId,
                            StringComparison.OrdinalIgnoreCase));

                    if (transaction is null)
                    {
                        throw new InvalidOperationException(
                            $"Transaction '{transactionId}' was not found.");
                    }

                    if (transaction.Status != PaymentStatus.Captured)
                    {
                        throw new InvalidOperationException(
                            $"Transaction '{transactionId}' cannot be refunded " +
                            $"because its status is {transaction.Status}.");
                    }

                    transaction.Status = PaymentStatus.Refunded;

                    return transactions;
                },
                () => new List<PaymentTransaction>());

            Log.Success(
                $"PaymentGateway -> Transaction {transactionId} refunded successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(
                $"PaymentGateway -> Refund failed for transaction {transactionId}: {ex.Message}");

            throw;
        }
    }
}
