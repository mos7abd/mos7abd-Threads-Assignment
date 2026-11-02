
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Utilities;

namespace OrderProcessing.Shared.Repositories;

public class SyncProductRepository
{
    private readonly JsonFileStore _store;

    public SyncProductRepository(JsonFileStore store)
    {
        _store = store;
    }

    public Product? GetById(int id)
    {
        Log.Info(
            "SyncProductRepository -> Opening connection...");

        Thread.Sleep(200);

        int queryDelay =
            SimulatedLatency.RandomMilliseconds(300, 600);

        Log.Info(
            $"SyncProductRepository -> Executing query for Product #{id}...");

        Thread.Sleep(queryDelay);

        List<Product> products =
            _store.Read<List<Product>>(DataFiles.Products)
            ?? new List<Product>();

        Product? product =
            products.FirstOrDefault(p => p.Id == id);

        if (product is not null)
        {
            Log.Success(
                $"SyncProductRepository -> Product #{product.Id} found ({product.Name})");
        }
        else
        {
            Log.Info(
                $"SyncProductRepository -> Product #{id} not found");
        }

        return product;
    }

    public bool ReserveStock(int productId, int qty)
    {
        if (qty <= 0)
        {
            Log.Error(
                $"SyncProductRepository -> Invalid quantity: {qty}");

            return false;
        }

        Log.Info(
            "SyncProductRepository -> Opening connection...");

        Thread.Sleep(200);

        int queryDelay =
            SimulatedLatency.RandomMilliseconds(300, 600);

        Log.Info(
            $"SyncProductRepository -> Reserving stock for Product #{productId}...");

        Thread.Sleep(queryDelay);

        bool reserved = false;

        _store.Update<List<Product>>(
            DataFiles.Products,
            products =>
            {
                Product? product =
                    products.FirstOrDefault(p => p.Id == productId);

                if (product is null ||
                    product.StockQuantity < qty)
                {
                    reserved = false;
                    return products;
                }

                product.StockQuantity -= qty;
                reserved = true;

                return products;
            },
            () => new List<Product>());

        if (!reserved)
        {
            Log.Info(
                $"SyncProductRepository -> Reservation failed for Product #{productId}. Product not found or insufficient stock.");

            return false;
        }

        Log.Success(
            $"SyncProductRepository -> Reserved {qty} units of Product #{productId}");

        return true;
    }
}
