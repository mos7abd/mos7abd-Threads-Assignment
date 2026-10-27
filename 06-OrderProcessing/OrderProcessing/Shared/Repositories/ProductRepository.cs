
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


}
