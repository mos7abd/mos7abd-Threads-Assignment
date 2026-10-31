
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

            if (coupon is null)
            {
                throw new InvalidCouponException(
                    $"Coupon '{order.CouponCode}' was not found.");
            }

            if (coupon.ExpiryDate < DateTime.Now)
            {
                throw new InvalidCouponException(
                    $"Coupon '{order.CouponCode}' has expired.");
            }

            couponDiscount = coupon.DiscountPercent;
        }
        decimal discountPercent = Math.Min(
       loyaltyDiscount + couponDiscount,
       30m);

        // Calculate the discount amount.
        decimal discount = Math.Round(
            order.SubTotal * discountPercent / 100m,
            2,
            MidpointRounding.AwayFromZero);

        // Store the discount amount in the order.
        order.Discount = discount;

        Log.Success(
            $"PricingService -> Discount calculated for Order #{order.Id}. " +
            $"Discount percentage: {discountPercent}%, " +
            $"Discount amount: {discount}");

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
        await CalculateTotalAsync(order, ct);
        //Log.Info(
        //    $"PricingService -> Pricing Order #{order.Id}...");

        //// Step 1: Calculate subtotal
        //await CalculateSubTotalAsync(order, ct);

        //// Step 2: Calculate discount
        //await GetDiscountAsync(order, ct);

        //// Step 3: Calculate shipping cost
        //await CalculateShippingCostAsync(order, ct);

        //// Step 4: Calculate final total
        //order.TotalAmount = Math.Round(
        //    order.SubTotal - order.Discount + order.ShippingCost,
        //    2,
        //    MidpointRounding.AwayFromZero);

        //Log.Success(
        //    $"PricingService -> Order #{order.Id} priced successfully. " +
        //    $"Subtotal: {order.SubTotal}, " +
        //    $"Discount: {order.Discount}, " +
        //    $"Shipping: {order.ShippingCost}, " +
        //    $"Total: {order.TotalAmount}");
    }

    public async Task<decimal> CalculateTotalAsync(
    Order order,
    CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        ct.ThrowIfCancellationRequested();

        if (order.Items is null || order.Items.Count == 0)
            throw new InvalidOperationException("Order has no items.");

        Log.Info($"PricingService -> Pricing Order #{order.Id}...");

        await SimulatedLatency.DelayAsync(
            "PricingService - Open connection",
            200,
            ct);

        await SimulatedLatency.DelayAsync(
            "PricingService - Execute query",
            SimulatedLatency.RandomMilliseconds(300, 600),
            ct);

        var products = await _store.ReadAsync<List<Product>>(
            DataFiles.Products,
            ct);

        if (products is null)
            throw new InvalidOperationException("Products file could not be loaded.");

        decimal subTotal = 0m;
        decimal totalQuantity = 0m;
        decimal totalWeight = 0m;

        foreach (var item in order.Items)
        {
            ct.ThrowIfCancellationRequested();

            if (item.Quantity <= 0)
                throw new InvalidOperationException("Item quantity must be positive.");

            var product = products.Find(p => p.Id == item.ProductId);

            if (product is null)
                throw new InvalidOperationException(
                    $"Product #{item.ProductId} was not found.");

            item.UnitPrice = product.Price;

            subTotal += item.UnitPrice * item.Quantity;
            totalQuantity += item.Quantity;
            totalWeight += product.WeightKg * item.Quantity;
        }

        order.SubTotal = RoundMoney(subTotal);

        // Start both operations before awaiting either.
        Task<decimal> discountTask = GetDiscountAsync(order, ct);

        Task<decimal> shippingTask = CalculateShippingCostFromValuesAsync(
            order,
            totalQuantity,
            totalWeight,
            ct);

        // Await separately; no group-wait API.
        order.Discount = RoundMoney(await discountTask);
        order.ShippingCost = RoundMoney(await shippingTask);

        ct.ThrowIfCancellationRequested();

        order.TotalAmount = RoundMoney(
            order.SubTotal - order.Discount + order.ShippingCost);

        Log.Success(
            $"Order #{order.Id}: SubTotal={order.SubTotal}, " +
            $"Discount={order.Discount}, Shipping={order.ShippingCost}, " +
            $"Total={order.TotalAmount}");

        return order.TotalAmount;
    }

    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private async Task<decimal> CalculateShippingCostFromValuesAsync(
    Order order,
    decimal totalQuantity,
    decimal totalWeight,
    CancellationToken ct)
    {
        Log.Info($"Connecting to Shipping API for Order #{order.Id}...");

        await SimulatedLatency.DelayAsync(
            "Shipping API - DNS/handshake",
            150,
            ct);

        if (_failShippingForOrderIds.Contains(order.Id))
            throw new ShippingProviderUnavailableException(
                "Shipping provider unavailable");

        await SimulatedLatency.DelayAsync(
            "Shipping API - Send request",
            300,
            ct);

        await SimulatedLatency.DelayAsync(
            "Shipping API - Wait for response",
            SimulatedLatency.RandomMilliseconds(500, 1500),
            ct);

        decimal shippingCost = order.SubTotal >= 1000m
            ? 0m
            : 5m + (1.50m * totalQuantity) + (2m * totalWeight);

        return RoundMoney(shippingCost);
    }
    public decimal CalculateTotal(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.Items is null || order.Items.Count == 0)
            throw new InvalidOperationException("Order has no items.");

        Log.Info($"PricingService -> Pricing Order #{order.Id}...");

        // Step 1: Open connection
        Thread.Sleep(200);

        Log.Info(
            "[PricingService - Open connection] Delay completed");

        // Step 2: Execute query
        Thread.Sleep(
            SimulatedLatency.RandomMilliseconds(300, 600));

        Log.Info(
            "[PricingService - Execute query] Delay completed");

        // Step 3: Load products
        List<Product> products =
            _store.Read<List<Product>>(DataFiles.Products)
            ?? throw new InvalidOperationException(
                "Products file could not be loaded.");

        decimal subTotal = 0m;
        decimal totalQuantity = 0m;
        decimal totalWeight = 0m;

        foreach (var item in order.Items)
        {
            if (item.Quantity <= 0)
                throw new InvalidOperationException(
                    "Item quantity must be positive.");

            Product? product =
                products.Find(p => p.Id == item.ProductId);

            if (product is null)
                throw new InvalidOperationException(
                    $"Product #{item.ProductId} was not found.");

            item.UnitPrice = product.Price;

            subTotal += item.UnitPrice * item.Quantity;
            totalQuantity += item.Quantity;
            totalWeight += product.WeightKg * item.Quantity;
        }

        order.SubTotal = RoundMoney(subTotal);

        // Step 4: Calculate discount
        order.Discount = RoundMoney(GetDiscount(order));

        // Step 5: Calculate shipping
        order.ShippingCost = RoundMoney(
            CalculateShippingCostFromValues(
                order,
                totalQuantity,
                totalWeight));

        // Step 6: Calculate final total
        order.TotalAmount = RoundMoney(
            order.SubTotal -
            order.Discount +
            order.ShippingCost);

        Log.Success(
            $"Order #{order.Id}: SubTotal={order.SubTotal}, " +
            $"Discount={order.Discount}, Shipping={order.ShippingCost}, " +
            $"Total={order.TotalAmount}");

        return order.TotalAmount;
    }

    private decimal GetDiscount(Order order)
    {
        Log.Info(
            $"PricingService -> Connecting to Discount API for Order #{order.Id}...");

        Thread.Sleep(150);

        Log.Info(
            "[Discount API - DNS/handshake] Delay completed");

        Log.Info(
            $"PricingService -> Sending discount request for Order #{order.Id}...");

        Thread.Sleep(300);

        Log.Info(
            "[Discount API - Send request] Delay completed");

        int responseDelay =
            SimulatedLatency.RandomMilliseconds(500, 1500);

        Log.Info(
            $"PricingService -> Waiting for discount response for Order #{order.Id}...");

        Thread.Sleep(responseDelay);

        Log.Info(
            "[Discount API - Wait for response] Delay completed");

        Customer? customer =
            _customerRepository.GetById(order.CustomerId);

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
            Coupon? coupon =
                _couponRepository.GetByCode(order.CouponCode);

            if (coupon is null)
            {
                throw new InvalidCouponException(
                    $"Coupon '{order.CouponCode}' was not found.");
            }

            if (coupon.ExpiryDate < DateTime.Now)
            {
                throw new InvalidCouponException(
                    $"Coupon '{order.CouponCode}' has expired.");
            }

            couponDiscount = coupon.DiscountPercent;
        }

        decimal discountPercent = Math.Min(
            loyaltyDiscount + couponDiscount,
            30m);

        decimal discount = RoundMoney(
            order.SubTotal * discountPercent / 100m);

        order.Discount = discount;

        Log.Success(
            $"PricingService -> Discount calculated for Order #{order.Id}. " +
            $"Discount percentage: {discountPercent}%, " +
            $"Discount amount: {discount}");

        return discount;
    }

    private decimal CalculateShippingCostFromValues(
    Order order,
    decimal totalQuantity,
    decimal totalWeight)
    {
        Log.Info(
            $"Connecting to Shipping API for Order #{order.Id}...");

        Thread.Sleep(150);

        Log.Info(
            "[Shipping API - DNS/handshake] Delay completed");

        if (_failShippingForOrderIds.Contains(order.Id))
        {
            throw new ShippingProviderUnavailableException(
                "Shipping provider unavailable");
        }

        Thread.Sleep(300);

        Log.Info(
            "[Shipping API - Send request] Delay completed");

        Thread.Sleep(
            SimulatedLatency.RandomMilliseconds(500, 1500));

        Log.Info(
            "[Shipping API - Wait for response] Delay completed");

        decimal shippingCost = order.SubTotal >= 1000m
            ? 0m
            : 5m + (1.50m * totalQuantity) + (2m * totalWeight);

        return RoundMoney(shippingCost);
    }


}
