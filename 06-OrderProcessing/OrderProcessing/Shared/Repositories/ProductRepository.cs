
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Utilities;

namespace OrderProcessing.Shared.Repositories;

public class ProductRepository
{
    private readonly JsonFileStore _store;

    public ProductRepository(JsonFileStore store)
    {
        _store = store;
    }

    public async Task<Product?> GetByIdAsync(
        int id,
        CancellationToken ct = default)
      {
        Log.Info("ProductRepository -> Opening connection...");

        await SimulatedLatency.DelayAsync(
            "ProductRepository - Open connection",
            200,
            ct);

        int queryDelay =
            SimulatedLatency.RandomMilliseconds(300, 600);

        Log.Info(
            $"ProductRepository -> Executing query for Product #{id}...");

        await SimulatedLatency.DelayAsync(
            "ProductRepository - Execute query",
            queryDelay,
            ct);

        List<Product> products =
            await _store.ReadAsync<List<Product>>(
                DataFiles.Products,
                ct)
            ?? new List<Product>();

        Product? product =
            products.FirstOrDefault(p => p.Id == id);

        if (product is not null)
        {
            Log.Success(
                $"ProductRepository -> Product #{product.Id} found ({product.Name})");
        }
        else
        {
            Log.Info(
                $"ProductRepository -> Product #{id} not found");
        }

        return product;
    }


    public async Task ReleaseStockAsync(
    int productId,
    int qty,
    CancellationToken ct = default)
    {
        if (qty <= 0)
        {
            Log.Error(
                $"ProductRepository -> Invalid release quantity: {qty}");

            return;
        }

        Log.Info("ProductRepository -> Opening connection...");

        await SimulatedLatency.DelayAsync(
            "ProductRepository - Open connection",
            200,
            ct);

        int queryDelay =
            SimulatedLatency.RandomMilliseconds(300, 600);

        Log.Info(
            $"ProductRepository -> Releasing stock for Product #{productId}...");

        await SimulatedLatency.DelayAsync(
            "ProductRepository - Execute query",
            queryDelay,
            ct);

        bool released = false;

        await _store.UpdateAsync<List<Product>>(
            DataFiles.Products,
            products =>
            {
                Product? product =
                    products.FirstOrDefault(p => p.Id == productId);

                if (product is null)
                {
                    released = false;
                    return products;
                }

                product.StockQuantity += qty;
                released = true;

                return products;
            },
            () => new List<Product>(),
            ct);

        if (!released)
        {
            Log.Error(
                $"ProductRepository -> Release failed for Product #{productId}. Product not found.");

            return;
        }

        Log.Success(
            $"ProductRepository -> Released {qty} units of Product #{productId}");
    }
    public async Task<bool> ReserveStockAsync(
        int productId,
        int qty,
        CancellationToken ct = default)
        {
            if (qty <= 0)
            {
                Log.Error(
                    $"ProductRepository -> Invalid quantity: {qty}");

                return false;
            }

            Log.Info("ProductRepository -> Opening connection...");

            await SimulatedLatency.DelayAsync(
                "ProductRepository - Open connection",
                200,
                ct);

            int queryDelay =
                SimulatedLatency.RandomMilliseconds(300, 600);

            Log.Info(
                $"ProductRepository -> Reserving stock for Product #{productId}...");

            await SimulatedLatency.DelayAsync(
                "ProductRepository - Execute query",
                queryDelay,
                ct);

            bool reserved = false;

            await _store.UpdateAsync<List<Product>>(
                DataFiles.Products,
                products =>
                {
                    Product? product =
                        products.FirstOrDefault(p => p.Id == productId);

                    if (product is null || product.StockQuantity < qty)
                    {
                        reserved = false;
                        return products;
                    }

                    product.StockQuantity -= qty;
                    reserved = true;

                    return products;
                },
                () => new List<Product>(),
                ct);

            if (!reserved)
            {
                Log.Info(
                    $"ProductRepository -> Reservation failed for Product #{productId}. Product not found or insufficient stock.");

                return false;
            }

            Log.Success(
                $"ProductRepository -> Reserved {qty} units of Product #{productId}");

            return true;
    }


    #region ReserveStockUnsafeAsync - Without Stock Protection

    public async Task<bool> ReserveStockUnsafeAsync(
        int productId,
        int qty,
        CancellationToken ct = default)
    {
        if (qty <= 0)
        {
            Log.Error(
                $"ProductRepository -> Invalid quantity: {qty}");

            return false;
        }

        Log.Info("ProductRepository -> Opening connection...");

        await SimulatedLatency.DelayAsync(
            "ProductRepository - Open connection",
            200,
            ct);

        List<Product> products =
            await _store.ReadAsync<List<Product>>(
                DataFiles.Products,
                ct)
            ?? new List<Product>();

        Product? product =
            products.FirstOrDefault(p => p.Id == productId);

        if (product is null || product.StockQuantity < qty)
        {
            Log.Info(
                $"ProductRepository -> Unsafe reservation failed for Product #{productId}.");

            return false;
        }

        int availableStock = product.StockQuantity;

        Log.Info(
            $"ProductRepository -> Beginning transaction for Product #{productId}...");

        await SimulatedLatency.DelayAsync(
            "ProductRepository - Begin transaction",
            100,
            ct);

        Log.Info(
            $"ProductRepository -> Updating stock for Product #{productId}...");

        await SimulatedLatency.DelayAsync(
            "ProductRepository - Update",
            400,
            ct);

        product.StockQuantity = availableStock - qty;

        await _store.WriteAsync(
            DataFiles.Products,
            products,
            ct);

        Log.Success(
            $"ProductRepository -> Unsafe reservation succeeded for Product #{productId}");

        return true;
    }

    #endregion



}
