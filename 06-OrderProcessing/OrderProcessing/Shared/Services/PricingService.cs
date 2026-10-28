
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderProcessing.Shared.Services;

public class PricingService
{
    private readonly JsonFileStore _store;
    private readonly ProductRepository _productRepository;
    private readonly CustomerRepository _customerRepository;
    private readonly CouponRepository _couponRepository;
    private readonly HashSet<int> _failShippingForOrderIds;

    public PricingService(
        JsonFileStore store,
        ProductRepository productRepository,
        CustomerRepository customerRepository,
        CouponRepository couponRepository,
        IEnumerable<int> failShippingForOrderIds)
    {
        _store = store;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _couponRepository = couponRepository;


        _failShippingForOrderIds =
            new HashSet<int>(failShippingForOrderIds);
    }


    #region #region CalculateSubTotalAsync - Without ProductRepository

    public async Task<decimal> CalculateSubTotalAsync(
        Order order,
        CancellationToken ct = default)
    {
        Log.Info(
            $"PricingService -> Opening database connection for Order #{order.Id}...");

        await SimulatedLatency.DelayAsync(
            "PricingService - Open connection",
            200,
            ct);

        int queryDelay =
            SimulatedLatency.RandomMilliseconds(300, 600);

        Log.Info(
            $"PricingService -> Executing pricing query for Order #{order.Id}...");

        await SimulatedLatency.DelayAsync(
            "PricingService - Execute query",
            queryDelay,
            ct);

        var products = await _store.ReadAsync<List<Product>>(
            DataFiles.Products,
            ct);

        if (products is null)
        {
            throw new InvalidOperationException(
                "Products file could not be loaded.");
        }

        decimal subTotal = 0m;

        foreach (var item in order.Items)
        {
            ct.ThrowIfCancellationRequested();

            var product = products.Find(
                p => p.Id == item.ProductId);

            if (product is null)
            {
                throw new InvalidOperationException(
                    $"Product #{item.ProductId} was not found.");
            }

            item.UnitPrice = product.Price;

            subTotal += product.Price * item.Quantity;
        }

        subTotal = Math.Round(
            subTotal,
            2,
            MidpointRounding.AwayFromZero);

        order.SubTotal = subTotal;

        return subTotal;
    }

   #endregion

     #region CalculateSubTotalAsync - Using ProductRepository
    //public async Task<decimal> CalculateSubTotalAsync(
    //    Order order,
    //    CancellationToken ct = default)
    //{
    //    Log.Info(
    //        $"PricingService -> Opening database connection for Order #{order.Id}...");

    //    await SimulatedLatency.DelayAsync(
    //        "PricingService - Open connection",
    //        200,
    //        ct);

    //    int queryDelay =
    //        SimulatedLatency.RandomMilliseconds(300, 600);

    //    Log.Info(
    //        $"PricingService -> Executing pricing query for Order #{order.Id}...");

    //    await SimulatedLatency.DelayAsync(
    //        "PricingService - Execute query",
    //        queryDelay,
    //        ct);

    //    decimal subTotal = 0m;

    //    foreach (var item in order.Items)
    //    {
    //        ct.ThrowIfCancellationRequested();

    //        var product = await _productRepository.GetByIdAsync(
    //            item.ProductId,
    //            ct);

    //        if (product is null)
    //        {
    //            throw new InvalidOperationException(
    //                $"Product #{item.ProductId} was not found.");
    //        }

    //        item.UnitPrice = product.Price;
    //        subTotal += product.Price * item.Quantity;
    //    }

    //    subTotal = Math.Round(
    //        subTotal,
    //        2,
    //        MidpointRounding.AwayFromZero);

    //    order.SubTotal = subTotal;

    //    return subTotal;
    //}
    #endregion


    public async Task<decimal> GetDiscountAsync(
        Order order,
        CancellationToken ct = default)
    {
        Log.Info(
            $"PricingService -> Connecting to Discount API for Order #{order.Id}...");

        await SimulatedLatency.DelayAsync(
            "Discount API - DNS/handshake",
            150,
            ct);

        Log.Info(
            $"PricingService -> Sending discount request for Order #{order.Id}...");

        await SimulatedLatency.DelayAsync(
            "Discount API - Send request",
            300,
            ct);

        int responseDelay =
            SimulatedLatency.RandomMilliseconds(500, 1500);

        Log.Info(
            $"PricingService -> Waiting for discount response for Order #{order.Id}...");

        await SimulatedLatency.DelayAsync(
            "Discount API - Wait for response",
            responseDelay,
            ct);

        var customer = await _customerRepository.GetByIdAsync(
            order.CustomerId,
            ct);

        if (customer is null)
        {
            throw new InvalidOperationException(
                $"Customer #{order.CustomerId} was not found.");
        }

        decimal loyaltyDiscount = customer.LoyaltyTier switch
        {
            LoyaltyTier.Bronze => 0m,
            LoyaltyTier.Silver => 5m,
            LoyaltyTier.Gold => 10m,
            _ => 0m
        };

        decimal couponDiscount = 0m;

        if (!string.IsNullOrWhiteSpace(order.CouponCode))
        {
            var coupon = await _couponRepository.GetByCodeAsync(
                order.CouponCode,
                ct);

            if (coupon is not null &&
                coupon.ExpiryDate >= DateTime.Now)
            {
                couponDiscount = coupon.DiscountPercent;
            }
        }

        decimal discountPercent = Math.Min(
            loyaltyDiscount + couponDiscount,
            30m);

        decimal discount = Math.Round(
            order.SubTotal * discountPercent / 100m,
            2,
            MidpointRounding.AwayFromZero);

        order.Discount = discount;

        return discount;
    }


    #region #region CalculateShippingCostAsync - Without ProductRepository

    public async Task<decimal> CalculateShippingCostAsync(
        Order order,
        CancellationToken ct = default)
    {
        Log.Info(
            $"PricingService -> Connecting to Shipping API for Order #{order.Id}...");

        await SimulatedLatency.DelayAsync(
            "Shipping API - DNS/handshake",
            150,
            ct);

        if (_failShippingForOrderIds.Contains(order.Id))
        {
            throw new ShippingProviderUnavailableException(
                "Shipping provider unavailable");
        }

        Log.Info(
            $"PricingService -> Sending shipping request for Order #{order.Id}...");

        await SimulatedLatency.DelayAsync(
            "Shipping API - Send request",
            300,
            ct);

        int responseDelay =
            SimulatedLatency.RandomMilliseconds(500, 1500);

        Log.Info(
            $"PricingService -> Waiting for shipping response for Order #{order.Id}...");

        await SimulatedLatency.DelayAsync(
            "Shipping API - Wait for response",
            responseDelay,
            ct);

        var products = await _store.ReadAsync<List<Product>>(
            DataFiles.Products,
            ct);

        if (products is null)
        {
            throw new InvalidOperationException(
                "Products file could not be loaded.");
        }

        decimal totalQuantity = 0m;
        decimal totalWeight = 0m;
        decimal subTotal = 0m;

        foreach (var item in order.Items)
        {
            ct.ThrowIfCancellationRequested();

            var product = products.Find(
                p => p.Id == item.ProductId);

            if (product is null)
            {
                throw new InvalidOperationException(
                    $"Product #{item.ProductId} was not found.");
            }

            item.UnitPrice = product.Price;

            totalQuantity += item.Quantity;
            totalWeight += product.WeightKg * item.Quantity;
            subTotal += product.Price * item.Quantity;
        }

        subTotal = Math.Round(
            subTotal,
            2,
            MidpointRounding.AwayFromZero);

        order.SubTotal = subTotal;

        decimal shippingCost = subTotal >= 1000m
            ? 0m
            : 5m + (1.50m * totalQuantity) + (2m * totalWeight);

        shippingCost = Math.Round(
            shippingCost,
            2,
            MidpointRounding.AwayFromZero);

        order.ShippingCost = shippingCost;

        return shippingCost;
    }

    #endregion

    #region CalculateShippingCostAsync - Using ProductRepository
    //public async Task<decimal> CalculateShippingCostAsync(
    //    Order order,
    //    CancellationToken ct = default)
    //{
    //    Log.Info(
    //        $"PricingService -> Connecting to Shipping API for Order #{order.Id}...");

    //    await SimulatedLatency.DelayAsync(
    //        "Shipping API - DNS/handshake",
    //        150,
    //        ct);

    //    if (_failShippingForOrderIds.Contains(order.Id))
    //    {
    //        throw new ShippingProviderUnavailableException(
    //            "Shipping provider unavailable");
    //    }

    //    Log.Info(
    //        $"PricingService -> Sending shipping request for Order #{order.Id}...");

    //    await SimulatedLatency.DelayAsync(
    //        "Shipping API - Send request",
    //        300,
    //        ct);

    //    int responseDelay =
    //        SimulatedLatency.RandomMilliseconds(500, 1500);

    //    Log.Info(
    //        $"PricingService -> Waiting for shipping response for Order #{order.Id}...");

    //    await SimulatedLatency.DelayAsync(
    //        "Shipping API - Wait for response",
    //        responseDelay,
    //        ct);

    //    decimal totalQuantity = 0m;
    //    decimal totalWeight = 0m;
    //    decimal subTotal = 0m;

    //    foreach (var item in order.Items)
    //    {
    //        ct.ThrowIfCancellationRequested();

    //        var product = await _productRepository.GetByIdAsync(
    //            item.ProductId,
    //            ct);

    //        if (product is null)
    //        {
    //            throw new InvalidOperationException(
    //                $"Product #{item.ProductId} was not found.");
    //        }

    //        item.UnitPrice = product.Price;

    //        totalQuantity += item.Quantity;
    //        totalWeight += product.WeightKg * item.Quantity;
    //        subTotal += item.UnitPrice * item.Quantity;
    //    }

    //    subTotal = Math.Round(
    //        subTotal,
    //        2,
    //        MidpointRounding.AwayFromZero);

    //    order.SubTotal = subTotal;

    //    decimal shippingCost = subTotal >= 1000m
    //        ? 0m
    //        : 5m + (1.50m * totalQuantity) + (2m * totalWeight);

    //    shippingCost = Math.Round(
    //        shippingCost,
    //        2,
    //        MidpointRounding.AwayFromZero);

    //    order.ShippingCost = shippingCost;

    //    return shippingCost;
    //}
    #endregion


    public async Task PriceOrderAsync(
    Order order,
    CancellationToken ct = default)
    {
        Log.Info(
            $"PricingService -> Pricing Order #{order.Id}...");

        // Step 1: Calculate subtotal
        await CalculateSubTotalAsync(order, ct);

        // Step 2: Calculate discount
        await GetDiscountAsync(order, ct);

        // Step 3: Calculate shipping cost
        await CalculateShippingCostAsync(order, ct);

        // Step 4: Calculate final total
        order.TotalAmount = Math.Round(
            order.SubTotal - order.Discount + order.ShippingCost,
            2,
            MidpointRounding.AwayFromZero);

        Log.Success(
            $"PricingService -> Order #{order.Id} priced successfully. " +
            $"Subtotal: {order.SubTotal}, " +
            $"Discount: {order.Discount}, " +
            $"Shipping: {order.ShippingCost}, " +
            $"Total: {order.TotalAmount}");
    }

}
