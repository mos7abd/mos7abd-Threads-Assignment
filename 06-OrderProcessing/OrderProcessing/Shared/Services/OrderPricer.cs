
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Shared.Services;

public class OrderPricer
{
    private readonly PricingService _pricingService;

    public OrderPricer(PricingService pricingService)
    {
        _pricingService = pricingService;
    }

    public async Task PriceOrderAsync(
        Order order,
        CancellationToken ct = default)
    {
        // Step 1: Calculate SubTotal first.
        await _pricingService.CalculateSubTotalAsync(order, ct);

        // Step 2: Start both operations before awaiting either one.
        Task<decimal> discountTask =
            _pricingService.GetDiscountAsync(order, ct);

        Task<decimal> shippingTask =
            _pricingService.CalculateShippingCostAsync(order, ct);

        // Step 3: Await each task individually.
        order.Discount = await discountTask;
        order.ShippingCost = await shippingTask;

        // Step 4: Calculate the final amount.
        order.TotalAmount = Math.Round(
            order.SubTotal - order.Discount + order.ShippingCost,
            2,
            MidpointRounding.AwayFromZero);
    }

    public async Task PriceOrderSequentialAsync(
        Order order,
        CancellationToken ct = default)
    {
        // Step 1: Calculate SubTotal first.
        await _pricingService.CalculateSubTotalAsync(order, ct);

        // Step 2: Start and await the discount operation.
        Task<decimal> discountTask =
            _pricingService.GetDiscountAsync(order, ct);

        order.Discount = await discountTask;

        // Step 3: Start and await shipping after discount completes.
        Task<decimal> shippingTask =
            _pricingService.CalculateShippingCostAsync(order, ct);

        order.ShippingCost = await shippingTask;

        // Step 4: Calculate the final amount.
        order.TotalAmount = Math.Round(
            order.SubTotal - order.Discount + order.ShippingCost,
            2,
            MidpointRounding.AwayFromZero);
    }
}
