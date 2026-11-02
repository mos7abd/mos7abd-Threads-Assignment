
using System;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using OrderProcessing.Shared.Utilities;

namespace OrderProcessing.Shared.Services;

public class OrderValidator
{
    private readonly CustomerRepository _customerRepository;
    private readonly ProductRepository _productRepository;
    private readonly CouponRepository _couponRepository;
    private readonly OrderRepository _orderRepository;

    public OrderValidator(
        CustomerRepository customerRepository,
        ProductRepository productRepository,
        CouponRepository couponRepository,
        OrderRepository orderRepository)
    {
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _couponRepository = couponRepository;
        _orderRepository = orderRepository;
    }

    public async Task<Order> ValidateOrderAsync(
        Order order,
        CancellationToken ct = default,
        bool saveResult = true)
    {
        Log.Info(
            $"OrderValidator -> Validating Order #{order.Id}...");

        // 1. Customer exists
        await SimulateValidationStepAsync(
            "Check customer exists", ct);

        var customer = await _customerRepository.GetByIdAsync(
            order.CustomerId, ct);

        if (customer is null)
        {
            return await RejectAsync(
                order,
                $"Customer #{order.CustomerId} not found",
                ct,
                saveResult);
        }

        Log.Success(
            $"OrderValidator -> Customer #{customer.Id} exists.");

        // 2. Customer is active
        await SimulateValidationStepAsync(
            "Check customer is active", ct);

        if (!customer.IsActive)
        {
            return await RejectAsync(
                order,
                "Customer is inactive",
                ct,
                saveResult);
        }

        Log.Success(
            $"OrderValidator -> Customer #{customer.Id} is active.");

        // 3. Every product exists
        // 4. StockQuantity >= Quantity
        foreach (var item in order.Items)
        {
            await SimulateValidationStepAsync(
                $"Check Product #{item.ProductId} exists", ct);

            var product = await _productRepository.GetByIdAsync(
                item.ProductId, ct);

            if (product is null)
            {
                return await RejectAsync(
                    order,
                    $"Product #{item.ProductId} not found",
                    ct,
                    saveResult);
            }

            Log.Success(
                $"OrderValidator -> Product #{product.Id} exists.");

            await SimulateValidationStepAsync(
                $"Check stock for '{product.Name}'", ct);

            if (product.StockQuantity < item.Quantity)
            {
                return await RejectAsync(
                    order,
                    $"Insufficient stock for '{product.Name}'",
                    ct,
                    saveResult);
            }

            Log.Success(
                $"OrderValidator -> Stock available for '{product.Name}'.");
        }

        // 5. Coupon exists, if supplied
        // 6. Coupon has not expired
        if (order.CouponCode is not null)
        {
            await SimulateValidationStepAsync(
                $"Check Coupon '{order.CouponCode}' exists", ct);

            var coupon = await _couponRepository.GetByCodeAsync(
                order.CouponCode, ct);

            if (coupon is null)
            {
                return await RejectAsync(
                    order,
                    $"Coupon '{order.CouponCode}' does not exist",
                    ct,
                    saveResult);
            }

            Log.Success(
                $"OrderValidator -> Coupon '{coupon.Code}' exists.");

            await SimulateValidationStepAsync(
                $"Check expiry for Coupon '{coupon.Code}'", ct);

            if (coupon.ExpiryDate.Date < DateTime.Today)
            {
                return await RejectAsync(
                    order,
                    $"Coupon '{order.CouponCode}' has expired",
                    ct,
                    saveResult);
            }

            Log.Success(
                $"OrderValidator -> Coupon '{coupon.Code}' has not expired.");
        }

        // All validation checks passed
        order.Status = OrderStatus.Validated;
        order.FailureReason = null;

        if (saveResult)
        {
            await _orderRepository.SaveAsync(order, ct);
        }

        Log.Success(
            $"OrderValidator -> Order #{order.Id} validated successfully.");

        return order;
    }

    private async Task<Order> RejectAsync(
        Order order,
        string failureReason,
        CancellationToken ct,
        bool saveResult)
    {
        order.Status = OrderStatus.Rejected;
        order.FailureReason = failureReason;

        Log.Error(
            $"OrderValidator -> Order #{order.Id} rejected: {failureReason}");

       if (saveResult)
{
    await _orderRepository.SaveAsync(order, ct);
}

        return order;
    }

    private async Task SimulateValidationStepAsync(
        string step,
        CancellationToken ct)
    {
        int delay =
            SimulatedLatency.RandomMilliseconds(300, 600);

        await SimulatedLatency.DelayAsync(
            $"OrderValidator - {step}",
            delay,
            ct);
    }
}
