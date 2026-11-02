
using System;
using System.Threading;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using OrderProcessing.Shared.Utilities;

namespace OrderProcessing.Phase1;

public class SyncOrderValidator
{
    private readonly SyncCustomerRepository _customerRepository;
    private readonly SyncProductRepository _productRepository;
    private readonly SyncCouponRepository _couponRepository;

    public SyncOrderValidator(
        SyncCustomerRepository customerRepository,
        SyncProductRepository productRepository,
        SyncCouponRepository couponRepository)
    {
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _couponRepository = couponRepository;
    }

    public Order ValidateOrder(Order order)
    {
        Log.Info(
            $"SyncOrderValidator -> Validating Order #{order.Id}...");

        // 1. Customer exists
        SimulateValidationStep("Check customer exists");

        var customer =
            _customerRepository.GetById(order.CustomerId);

        if (customer is null)
        {
            return Reject(
                order,
                $"Customer #{order.CustomerId} not found");
        }

        Log.Success(
            $"SyncOrderValidator -> Customer #{customer.Id} exists.");

        // 2. Customer is active
        SimulateValidationStep("Check customer is active");

        if (!customer.IsActive)
        {
            return Reject(
                order,
                "Customer is inactive");
        }

        Log.Success(
            $"SyncOrderValidator -> Customer #{customer.Id} is active.");

        // 3. Every product exists
        // 4. StockQuantity >= Quantity
        foreach (var item in order.Items)
        {
            SimulateValidationStep(
                $"Check Product #{item.ProductId} exists");

            var product =
                _productRepository.GetById(item.ProductId);

            if (product is null)
            {
                return Reject(
                    order,
                    $"Product #{item.ProductId} not found");
            }

            Log.Success(
                $"SyncOrderValidator -> Product #{product.Id} exists.");

            SimulateValidationStep(
                $"Check stock for '{product.Name}'");

            if (product.StockQuantity < item.Quantity)
            {
                return Reject(
                    order,
                    $"Insufficient stock for '{product.Name}'");
            }

            Log.Success(
                $"SyncOrderValidator -> Stock available for '{product.Name}'.");
        }

        // 5. Coupon exists, if supplied
        // 6. Coupon has not expired
        if (order.CouponCode is not null)
        {
            SimulateValidationStep(
                $"Check Coupon '{order.CouponCode}' exists");

            var coupon =
                _couponRepository.GetByCode(order.CouponCode);

            if (coupon is null)
            {
                return Reject(
                    order,
                    $"Coupon '{order.CouponCode}' does not exist");
            }

            Log.Success(
                $"SyncOrderValidator -> Coupon '{coupon.Code}' exists.");

            SimulateValidationStep(
                $"Check expiry for Coupon '{coupon.Code}'");

            if (coupon.ExpiryDate.Date < DateTime.Today)
            {
                return Reject(
                    order,
                    $"Coupon '{order.CouponCode}' has expired");
            }

            Log.Success(
                $"SyncOrderValidator -> Coupon '{coupon.Code}' has not expired.");
        }

        // All validation checks passed
        order.Status = OrderStatus.Validated;
        order.FailureReason = null;

        Log.Success(
            $"SyncOrderValidator -> Order #{order.Id} validated successfully.");

        return order;
    }

    private Order Reject(
        Order order,
        string failureReason)
    {
        order.Status = OrderStatus.Rejected;
        order.FailureReason = failureReason;

        Log.Error(
            $"SyncOrderValidator -> Order #{order.Id} rejected: {failureReason}");

        return order;
    }

    private void SimulateValidationStep(string step)
    {
        int delay =
            SimulatedLatency.RandomMilliseconds(300, 600);

        Log.Info(
            $"SyncOrderValidator -> {step}, waiting {delay}ms...");

        Thread.Sleep(delay);
    }
}
