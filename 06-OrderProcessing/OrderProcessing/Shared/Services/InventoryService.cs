using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Utilities;

namespace OrderProcessing.Shared.Services;

public class InventoryService
{
    private readonly JsonFileStore _store;

    public InventoryService(JsonFileStore store)
    {
        _store = store;
    }

    public async Task<bool> ReserveStockAsync(
        Order order,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.Items.Any(item => item.Quantity <= 0))
        {
            throw new ArgumentException(
                "Order quantities must be greater than zero.",
                nameof(order));
        }

        Log.Info(
            $"InventoryService -> Reserving stock for Order #{order.Id}...");

        // Step 1: Open connection
        Log.Info("InventoryService -> Opening connection...");

        await SimulatedLatency.DelayAsync(
            "InventoryService - Open connection",
            200,
            ct);

        // Step 2: Begin transaction
        Log.Info("InventoryService -> Beginning transaction...");

        await SimulatedLatency.DelayAsync(
            "InventoryService - Begin transaction",
            100,
            ct);

        // Step 3: Update stock atomically
        Log.Info("InventoryService -> Updating stock...");

        await SimulatedLatency.DelayAsync(
            "InventoryService - Update stock",
            400,
            ct);

        bool reserved = false;

        await _store.UpdateAsync<List<Product>>(
            DataFiles.Products,
            products =>
            {
                ct.ThrowIfCancellationRequested();

                // Combine duplicate product IDs into one requirement.
                var requiredQuantities = order.Items
                    .GroupBy(item => item.ProductId)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Sum(item => item.Quantity));

                // Validate every product before changing any stock.
                foreach (var requirement in requiredQuantities)
                {
                    var product = products.Find(
                        p => p.Id == requirement.Key);

                    if (product is null ||
                        product.StockQuantity < requirement.Value)
                    {
                        reserved = false;
                        return products;
                    }
                }

                // All products are available: deduct all quantities.
                foreach (var requirement in requiredQuantities)
                {
                    var product = products.Find(
                        p => p.Id == requirement.Key)!;

                    product.StockQuantity -= requirement.Value;
                }

                reserved = true;
                return products;
            },
            () => new List<Product>(),
            ct);

        // Step 4: Commit
        Log.Info("InventoryService -> Committing transaction...");

        await SimulatedLatency.DelayAsync(
            "InventoryService - Commit",
            200,
            ct);

        if (!reserved)
        {
            Log.Info(
                $"InventoryService -> Reservation failed for Order #{order.Id}. " +
                "A product was missing or stock was insufficient.");

            return false;
        }

        Log.Success(
            $"InventoryService -> Stock reserved for Order #{order.Id}.");

        return true;
    }


    public async Task ReleaseStockAsync(
        Order order,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.Items is null || order.Items.Count == 0)
        {
            throw new ArgumentException(
                "Order must contain at least one item.",
                nameof(order));
        }

        if (order.Items.Any(item =>
            item is null ||
            item.ProductId <= 0 ||
            item.Quantity <= 0))
        {
            throw new ArgumentException(
                "Every order item must have a valid product ID and positive quantity.",
                nameof(order));
        }

        ct.ThrowIfCancellationRequested();

        Log.Info(
            $"InventoryService -> Releasing stock for Order {order.Id}...");

        try
        {
            // Open connection
            await SimulatedLatency.DelayAsync(
                "InventoryService - Open connection",
                200,
                ct);

            // Begin transaction
            await SimulatedLatency.DelayAsync(
                "InventoryService - Begin transaction",
                100,
                ct);

            // Update stock
            await SimulatedLatency.DelayAsync(
                "InventoryService - Update stock",
                400,
                ct);

            await _store.UpdateAsync<List<Product>>(
                DataFiles.Products,
                products =>
                {
                    // Validate all products before changing stock.
                    foreach (var item in order.Items)
                    {
                        if (!products.Any(
                            product => product.Id == item.ProductId))
                        {
                            throw new InvalidOperationException(
                                $"Product {item.ProductId} was not found.");
                        }
                    }

                    // Add every item's quantity back.
                    foreach (var item in order.Items)
                    {
                        var product = products.First(
                            product => product.Id == item.ProductId);

                        product.StockQuantity += item.Quantity;
                    }

                    return products;
                },
                () => new List<Product>(),
                ct);

            // Commit
            await SimulatedLatency.DelayAsync(
                "InventoryService - Commit",
                200,
                ct);

            Log.Success(
                $"InventoryService -> Stock released successfully for Order {order.Id}.");
        }
        catch (OperationCanceledException)
        {
            Log.Info(
                $"InventoryService -> Stock release cancelled for Order {order.Id}.");

            throw;
        }
        catch (Exception ex)
        {
            Log.Error(
                $"InventoryService -> Failed to release stock for Order {order.Id}: {ex.Message}");

            throw;
        }
    }
    public bool ReserveStock(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.Items.Any(item => item.Quantity <= 0))
        {
            throw new ArgumentException(
                "Order quantities must be greater than zero.",
                nameof(order));
        }

        Log.Info(
            $"InventoryService -> Reserving stock for Order #{order.Id}...");

        // Step 1: Open connection
        Log.Info("InventoryService -> Opening connection...");

        Thread.Sleep(200);

        Log.Info(
            "[InventoryService - Open connection] Delay completed");

        // Step 2: Begin transaction
        Log.Info("InventoryService -> Beginning transaction...");

        Thread.Sleep(100);

        Log.Info(
            "[InventoryService - Begin transaction] Delay completed");

        // Step 3: Update stock atomically
        Log.Info("InventoryService -> Updating stock...");

        Thread.Sleep(400);

        Log.Info(
            "[InventoryService - Update stock] Delay completed");

        bool reserved = false;

        _store.Update<List<Product>>(
            DataFiles.Products,
            products =>
            {
                // Combine duplicate product IDs into one requirement.
                var requiredQuantities = order.Items
                    .GroupBy(item => item.ProductId)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Sum(item => item.Quantity));

                // Validate every product before changing any stock.
                foreach (var requirement in requiredQuantities)
                {
                    var product = products.Find(
                        p => p.Id == requirement.Key);

                    if (product is null ||
                        product.StockQuantity < requirement.Value)
                    {
                        reserved = false;
                        return products;
                    }
                }

                // All products are available: deduct all quantities.
                foreach (var requirement in requiredQuantities)
                {
                    var product = products.Find(
                        p => p.Id == requirement.Key)!;

                    product.StockQuantity -= requirement.Value;
                }

                reserved = true;
                return products;
            },
            () => new List<Product>());

        // Step 4: Commit
        Log.Info("InventoryService -> Committing transaction...");

        Thread.Sleep(200);

        Log.Info(
            "[InventoryService - Commit] Delay completed");

        if (!reserved)
        {
            Log.Info(
                $"InventoryService -> Reservation failed for Order #{order.Id}. " +
                "A product was missing or stock was insufficient.");

            return false;
        }

        Log.Success(
            $"InventoryService -> Stock reserved for Order #{order.Id}.");

        return true;
    }


    public void ReleaseStock(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.Items is null || order.Items.Count == 0)
        {
            throw new ArgumentException(
                "Order must contain at least one item.",
                nameof(order));
        }

        if (order.Items.Any(item =>
            item is null ||
            item.ProductId <= 0 ||
            item.Quantity <= 0))
        {
            throw new ArgumentException(
                "Every order item must have a valid product ID and positive quantity.",
                nameof(order));
        }

        Log.Info(
            $"InventoryService -> Releasing stock for Order {order.Id}...");

        try
        {
            // Open connection
            Thread.Sleep(200);

            Log.Info(
                "[InventoryService - Open connection] Delay completed");

            // Begin transaction
            Thread.Sleep(100);

            Log.Info(
                "[InventoryService - Begin transaction] Delay completed");

            // Update stock
            Thread.Sleep(400);

            Log.Info(
                "[InventoryService - Update stock] Delay completed");

            _store.Update<List<Product>>(
                DataFiles.Products,
                products =>
                {
                    // Validate all products before changing stock.
                    foreach (var item in order.Items)
                    {
                        if (!products.Any(
                            product => product.Id == item.ProductId))
                        {
                            throw new InvalidOperationException(
                                $"Product {item.ProductId} was not found.");
                        }
                    }

                    // Add every item's quantity back.
                    foreach (var item in order.Items)
                    {
                        var product = products.First(
                            product => product.Id == item.ProductId);

                        product.StockQuantity += item.Quantity;
                    }

                    return products;
                },
                () => new List<Product>());

            // Commit
            Thread.Sleep(200);

            Log.Info(
                "[InventoryService - Commit] Delay completed");

            Log.Success(
                $"InventoryService -> Stock released successfully for Order {order.Id}.");
        }
        catch (Exception ex)
        {
            Log.Error(
                $"InventoryService -> Failed to release stock for Order {order.Id}: {ex.Message}");

            throw;
        }
    }

}