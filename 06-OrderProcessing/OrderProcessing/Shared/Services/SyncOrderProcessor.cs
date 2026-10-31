using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using OrderProcessing.Shared.Services;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Services;

public sealed class SyncOrderProcessor
{
    private const int PaymentTimeoutMs = 3000;
    private const string CancelledReason = "Cancelled";

    private readonly CustomerService _customerService;
    private readonly InventoryService _inventoryService;
    private readonly PricingService _pricingService;
    private readonly PaymentGateway _paymentGateway;
    private readonly OrderRepository _orderRepository;
    private readonly EmailService _emailService;
    private readonly ProductRepository _productRepository;
    private readonly JsonFileStore _store;

    public SyncOrderProcessor(
        CustomerService customerService,
        InventoryService inventoryService,
        PricingService pricingService,
        PaymentGateway paymentGateway,
        OrderRepository orderRepository,
        EmailService emailService,
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

    public bool ProcessOrder(Order order)
    {
        bool stockReserved = false;
        string? transactionId = null;
        Customer? customer = null;

        try
        {
            // Step 1: Get customer
            customer = ExecuteStep(
                order,
                "Step 1/6 Customer",
                () =>
                {
                    Customer result =
                        _customerService.GetCustomer(order.CustomerId);

                    if (!result.IsActive)
                    {
                        throw new InvalidOperationException(
                            "Customer is inactive");
                    }

                    return result;
                });

            order.Status = OrderStatus.Validated;

            // Step 2: Reserve stock
            bool reserved = ExecuteStep(
                order,
                "Step 2/6 Inventory",
                () => _inventoryService.ReserveStock(order));

            if (!reserved)
            {
                string productName =
                    GetUnavailableProductName(order);

                order.Status = OrderStatus.Rejected;
                order.FailureReason =
                    $"Insufficient stock for '{productName}'";

                SaveOrder(order);
                return false;
            }

            stockReserved = true;

            // Step 3: Calculate total
            try
            {
                ExecuteStep(
                    order,
                    "Step 3/6 Pricing",
                    () =>
                    {
                        _pricingService.CalculateTotal(order);
                        return true;
                    });
            }
            catch (InvalidCouponException ex)
            {
                ReleaseStock(order);
                stockReserved = false;

                order.Status = OrderStatus.Rejected;
                order.FailureReason = ex.Message;

                SaveOrder(order);
                return false;
            }

            // Step 4: Process payment
            PaymentResult? paymentResult =
                ProcessPaymentWithTimeout(order);

            if (paymentResult is null)
            {
                ReleaseStock(order);
                stockReserved = false;

                order.Status = OrderStatus.PaymentFailed;
                order.FailureReason =
                    "Payment gateway timeout (3s).";

                SaveOrder(order);
                return false;
            }

            if (!paymentResult.Success ||
                paymentResult.Status != PaymentStatus.Captured)
            {
                ReleaseStock(order);
                stockReserved = false;

                order.Status = OrderStatus.PaymentFailed;
                order.FailureReason =
                    "Card declined by issuer.";

                SaveOrder(order);
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

                ExecuteStep(
                    order,
                    "Step 5/6 Save order",
                    () =>
                    {
                        _orderRepository.Save(order);
                        return true;
                    });
            }
            catch (DatabaseWriteException)
            {
                RefundPayment(transactionId);
                transactionId = null;

                ReleaseStock(order);
                stockReserved = false;

                order.Status = OrderStatus.Failed;
                order.FailureReason =
                    $"Database save failed after payment; payment {order.TransactionId} refunded";

                SaveOrder(order);
                return false;
            }

            // Step 6: Send confirmation email
            // Email failure must not change the order status.
            try
            {
                ExecuteStep(
                    order,
                    "Step 6/6 Email",
                    () =>
                    {
                        _emailService.SendConfirmation(order, customer);
                        return true;
                    });
            }
            catch (Exception ex)
            {
                Log.Error(
                    $"Order #{order.Id} -> Email failed: {ex.Message}");
            }

            // Stock remains reserved after successful completion.
            stockReserved = false;

            return order.Status == OrderStatus.Completed;
        }
        catch (CustomerNotFoundException ex)
        {
            order.Status = OrderStatus.Rejected;
            order.FailureReason = ex.Message;

            SaveOrder(order);
            return false;
        }
        catch (InvalidOperationException ex)
            when (ex.Message == "Customer is inactive")
        {
            order.Status = OrderStatus.Rejected;
            order.FailureReason = "Customer is inactive";

            SaveOrder(order);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(
                $"Order #{order.Id} -> Processing failed: {ex.Message}");

            if (stockReserved)
            {
                ReleaseStock(order);
                stockReserved = false;
            }

            order.Status = OrderStatus.Failed;
            order.FailureReason = ex.Message;

            SaveOrder(order);
            return false;
        }
    }

    private PaymentResult? ProcessPaymentWithTimeout(Order order)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            PaymentResult result =
                _paymentGateway.ProcessPayment(
                    order.Id,
                    order.TotalAmount);

            stopwatch.Stop();

            Log.Info(
                $"Order #{order.Id} -> Payment completed in " +
                $"{stopwatch.ElapsedMilliseconds} ms");

            if (stopwatch.ElapsedMilliseconds > PaymentTimeoutMs)
            {
                Log.Error(
                    $"Order #{order.Id} -> Payment TIMEOUT after " +
                    $"{stopwatch.ElapsedMilliseconds} ms");

                return null;
            }

            return result;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            throw;
        }
    }

    private string? FindCapturedTransaction(int orderId)
    {
        List<PaymentTransaction> transactions =
            _store.Read<List<PaymentTransaction>>(
                DataFiles.Payments);

        PaymentTransaction? transaction =
            transactions.LastOrDefault(
                payment =>
                    payment.OrderId == orderId &&
                    payment.Status == PaymentStatus.Captured);

        return transaction?.TransactionId;
    }

    private void RefundPayment(string transactionId)
    {
        _paymentGateway.Refund(transactionId);
    }

    private void ReleaseStock(Order order)
    {
        _inventoryService.ReleaseStock(order);
    }

    private void SaveOrder(Order order)
    {
        _orderRepository.Save(order);
    }

    private string GetUnavailableProductName(Order order)
    {
        foreach (OrderItem item in order.Items)
        {
            Product? product =
                _productRepository.GetById(item.ProductId);

            if (product is not null &&
                product.StockQuantity < item.Quantity)
            {
                return product.Name;
            }
        }

        return "Unknown product";
    }

    private static T ExecuteStep<T>(
        Order order,
        string stepName,
        Func<T> operation)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            T result = operation();

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