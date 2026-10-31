
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Interfaces;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using OrderProcessing.Shared.Services;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Services;

public sealed class OrderProcessor
{
    private const int PaymentTimeoutMs = 3000;
    private const string CancelledReason = "Cancelled";

    private readonly CustomerService _customerService;
    private readonly InventoryService _inventoryService;
    private readonly PricingService _pricingService;
    private readonly IPaymentGateway _paymentGateway;
    private readonly OrderRepository _orderRepository;
    private readonly IEmailService _emailService;
    private readonly ProductRepository _productRepository;
    private readonly JsonFileStore _store;

    public OrderProcessor(
        CustomerService customerService,
        InventoryService inventoryService,
        PricingService pricingService,
        IPaymentGateway paymentGateway,
        OrderRepository orderRepository,
        IEmailService emailService,
        ProductRepository productRepository,
        JsonFileStore store,
        bool failuresEnabled = true)
    {
        _customerService = customerService;
        _inventoryService = inventoryService;
        _pricingService = pricingService;
        _paymentGateway = paymentGateway;
        _orderRepository = orderRepository;
        _emailService = emailService;
        _productRepository = productRepository;
        _store = store;
    }

    public async Task<bool> ProcessOrderAsync(
        Order order,
        CancellationToken ct = default)
    {
        bool stockReserved = false;
        string? transactionId = null;
        Customer? customer = null;

        try
        {
            ct.ThrowIfCancellationRequested();

            // Step 1: Get customer
            customer = await ExecuteStepAsync(
                order,
                "Step 1/6 Customer",
                async () =>
                {
                    Customer result =
                        await _customerService.GetCustomerAsync(
                            order.CustomerId, ct);

                    if (!result.IsActive)
                    {
                        throw new InvalidOperationException(
                            "Customer is inactive");
                    }

                    return result;
                },
                ct);

            order.Status = OrderStatus.Validated;

            // Step 2: Reserve stock
            bool reserved = await ExecuteStepAsync(
                order,
                "Step 2/6 Inventory",
                () => _inventoryService.ReserveStockAsync(order, ct),
                ct);

            if (!reserved)
            {
                string productName =
                    await GetUnavailableProductNameAsync(order, ct);

                order.Status = OrderStatus.Rejected;
                order.FailureReason =
                    $"Insufficient stock for '{productName}'";

                await SaveOrderAsync(order, ct);
                return false;
            }

            stockReserved = true;

            // Step 3: Calculate total
            try
            {
                await ExecuteStepAsync(
                    order,
                    "Step 3/6 Pricing",
                    async () =>
                    {
                        await _pricingService.CalculateTotalAsync(
                            order, ct);
                        return true;
                    },
                    ct);
            }
            catch (InvalidCouponException ex)
            {
                await ReleaseStockAsync(order);
                stockReserved = false;

                order.Status = OrderStatus.Rejected;
                order.FailureReason = ex.Message;

                await SaveOrderAsync(order, ct);
                return false;
            }

            // Step 4: Process payment
            PaymentResult? paymentResult =
                await ProcessPaymentWithTimeoutAsync(order, ct);

            if (paymentResult is null)
            {
                await ReleaseStockAsync(order);
                stockReserved = false;

                order.Status = OrderStatus.PaymentFailed;
                order.FailureReason =
                    "Payment gateway timeout (3s).";

                await SaveOrderAsync(order, ct);
                return false;
            }

            if (!paymentResult.Success ||
                paymentResult.Status != PaymentStatus.Captured)
            {
                await ReleaseStockAsync(order);
                stockReserved = false;

                order.Status = OrderStatus.PaymentFailed;
                order.FailureReason =
                    "Card declined by issuer.";

                await SaveOrderAsync(order, ct);
                return false;
            }

            transactionId = paymentResult.TransactionId;

            if (string.IsNullOrWhiteSpace(transactionId))
            {
                throw new InvalidOperationException(
                    "Payment was captured without a transaction ID.");
            }

            order.TransactionId = transactionId;

            // Step 5: Save completed order
            try
            {
                order.Status = OrderStatus.Completed;
                order.FailureReason = null;

                await ExecuteStepAsync(
                    order,
                    "Step 5/6 Save order",
                    async () =>
                    {
                        await _orderRepository.SaveAsync(order, ct);
                        return true;
                    },
                    ct);
            }
            catch (DatabaseWriteException)
            {
                await RefundPaymentAsync(transactionId);
                transactionId = null;

                await ReleaseStockAsync(order);
                stockReserved = false;

                order.Status = OrderStatus.Failed;
                order.FailureReason =
                    $"Database save failed after payment; payment {order.TransactionId} refunded";

                await SaveOrderAsync(order, ct);
                return false;
            }

            // Step 6: Send confirmation email
            // Email failure must not change the order status.
            try
            {
                await ExecuteStepAsync(
                    order,
                    "Step 6/6 Email",
                    async () =>
                    {
                        await _emailService.SendConfirmationAsync(
                            order, customer, ct);
                        return true;
                    },
                    ct);
            }
            catch (OperationCanceledException)
                when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(
                    $"Order #{order.Id} -> Email failed: {ex.Message}");
            }

            // The stock reservation remains valid after successful completion.
            stockReserved = false;

            return order.Status == OrderStatus.Completed;
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            await HandleCancellationAsync(
                order,
                stockReserved,
                transactionId);

            throw;
        }
        catch (CustomerNotFoundException ex)
        {
            order.Status = OrderStatus.Rejected;
            order.FailureReason = ex.Message;

            await SaveOrderAsync(order, ct);
            return false;
        }
        catch (InvalidOperationException ex)
            when (ex.Message == "Customer is inactive")
        {
            order.Status = OrderStatus.Rejected;
            order.FailureReason = "Customer is inactive";

            await SaveOrderAsync(order, ct);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(
                $"Order #{order.Id} -> Processing failed: {ex.Message}");

            if (stockReserved)
            {
                await ReleaseStockAsync(order);
                stockReserved = false;
            }

            order.Status = OrderStatus.Failed;
            order.FailureReason = ex.Message;

            await SaveOrderAsync(order, ct);
            return false;
        }
    }
    public async Task ProcessOrderFireAndForgetAsync(
    Order order,
    Customer customer)
    {
        await _orderRepository.SaveAsync(order);

#pragma warning disable CS4014
        _emailService.SendConfirmationAsync(order, customer);
#pragma warning restore CS4014

        Console.WriteLine("Order processing finished");
    }

    public async Task ProcessOrderWithAwaitAsync(
    Order order,
    Customer customer)
    {
        await _orderRepository.SaveAsync(order);

        await _emailService.SendConfirmationAsync(order, customer);

        Console.WriteLine("Order processing finished");
    }
    private async Task<PaymentResult?> ProcessPaymentWithTimeoutAsync(
        Order order,
        CancellationToken runToken)
    {
        using var timeoutCts =
            new CancellationTokenSource(PaymentTimeoutMs);

        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                runToken, timeoutCts.Token);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            PaymentResult result =
                await _paymentGateway.ProcessPaymentAsync(
                    order.Id,
                    order.TotalAmount,
                    linkedCts.Token);

            stopwatch.Stop();

            Log.Info(
                $"Order #{order.Id} -> Payment completed in " +
                $"{stopwatch.ElapsedMilliseconds} ms");

            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();

            // Run cancellation takes priority over the local timeout.
            if (runToken.IsCancellationRequested)
            {
                throw;
            }

            if (timeoutCts.IsCancellationRequested)
            {
                Log.Error(
                    $"Order #{order.Id} -> Payment TIMEOUT after " +
                    $"{stopwatch.ElapsedMilliseconds} ms");

                return null;
            }

            throw;
        }
    }

    private async Task HandleCancellationAsync(
        Order order,
        bool stockReserved,
        string? transactionId)
    {
        Log.Info(
            $"Order #{order.Id} -> Handling cancellation compensation...");

        // Try to recover a captured payment even if the gateway was
        // cancelled after writing the transaction but before returning it.
        string? capturedTransactionId =
            transactionId ?? await FindCapturedTransactionAsync(order.Id);

        if (!string.IsNullOrWhiteSpace(capturedTransactionId))
        {
            await RefundPaymentAsync(capturedTransactionId);
        }

        if (stockReserved)
        {
            await ReleaseStockAsync(order);
        }

        order.Status = OrderStatus.Pending;
        order.FailureReason = CancelledReason;

        // Compensation must not use the already-cancelled run token.
        await SaveOrderAsync(order, CancellationToken.None);

        Log.Success(
            $"Order #{order.Id} -> Cancellation compensation completed.");
    }

    private async Task<string?> FindCapturedTransactionAsync(int orderId)
    {
        List<PaymentTransaction> transactions =
            await _store.ReadAsync<List<PaymentTransaction>>(
                DataFiles.Payments,
                CancellationToken.None);

        PaymentTransaction? transaction =
            transactions.LastOrDefault(
                payment =>
                    payment.OrderId == orderId &&
                    payment.Status == PaymentStatus.Captured);

        return transaction?.TransactionId;
    }

    private async Task RefundPaymentAsync(string transactionId)
    {
        await _paymentGateway.RefundAsync(
            transactionId,
            CancellationToken.None);
    }

    private async Task ReleaseStockAsync(Order order)
    {
        await _inventoryService.ReleaseStockAsync(
            order,
            CancellationToken.None);
    }

    private async Task SaveOrderAsync(
        Order order,
        CancellationToken ct)
    {
        await _orderRepository.SaveAsync(order, ct);
    }

    private async Task<string> GetUnavailableProductNameAsync(
        Order order,
        CancellationToken ct)
    {
        foreach (OrderItem item in order.Items)
        {
            Product? product =
                await _productRepository.GetByIdAsync(
                    item.ProductId, ct);

            if (product is not null &&
                product.StockQuantity < item.Quantity)
            {
                return product.Name;
            }
        }

        return "Unknown product";
    }

    private static async Task<T> ExecuteStepAsync<T>(
        Order order,
        string stepName,
        Func<Task<T>> operation,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var stopwatch = Stopwatch.StartNew();

        try
        {
            T result = await operation();

            stopwatch.Stop();

            Log.Success(
                $"Order #{order.Id} -> {stepName} OK in " +
                $"{stopwatch.ElapsedMilliseconds} ms");

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Log.Error(
                $"Order #{order.Id} -> {stepName} FAILED in " +
                $"{stopwatch.ElapsedMilliseconds} ms: {ex.Message}");

            throw;
        }
    }
}
