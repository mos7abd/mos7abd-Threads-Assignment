
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase2;

public class RaceConditionDemo
{
    private readonly JsonFileStore _store;
    private readonly ProductRepository _productRepository;

    public RaceConditionDemo(
        JsonFileStore store,
        ProductRepository productRepository)
    {
        _store = store;
        _productRepository = productRepository;
    }

    public async Task RunAsync(
        CancellationToken ct = default)
    {
        // 1. Reset P4 stock to exactly one unit.
        await _store.UpdateAsync<List<Product>>(
            "products.json",
            products =>
            {
                Product? product =
                    products.FirstOrDefault(p => p.Id == 4);

                if (product is null)
                {
                    throw new InvalidOperationException(
                        "Product #4 was not found.");
                }

                product.StockQuantity = 1;

                return products;
            },
            () => new List<Product>(),
            ct);

        Log.Info("P4 stock reset to 1.");

        // 2. Start both unsafe reservations concurrently.
        Task<bool> order10Task =
           // _productRepository.ReserveStockUnsafeAsync(4, 1, ct);
           _productRepository.ReserveStockAsync(4, 1, ct);


        Task<bool> order11Task =
           // _productRepository.ReserveStockUnsafeAsync(4, 1, ct); // Without protection (Race Condition Demo)
           _productRepository.ReserveStockAsync(4, 1, ct);         // With protection (After Fix)

        // 3. Await each task separately.
        bool order10Result = await order10Task;
        bool order11Result = await order11Task;

        // 4. Read the actual stock from the file.
        List<Product> products =
            await _store.ReadAsync<List<Product>>(
                "products.json",
                ct)
            ?? new List<Product>();

        Product? finalProduct =
            products.FirstOrDefault(p => p.Id == 4);

       // Console.WriteLine("\n=== Race Condition Demo ===");
        Console.WriteLine("\n=== Race Condition Demo - After Fix ===");
        Console.WriteLine(
            $"Order #10 reservation: {order10Result}");
        Console.WriteLine(
            $"Order #11 reservation: {order11Result}");
        Console.WriteLine(
            $"Final P4 stock: {finalProduct?.StockQuantity}");
    }
}
