
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Shared.Storage;

public sealed class DataSeeder
{
    private readonly JsonFileStore _store;

    public DataSeeder(JsonFileStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    private static readonly Customer[] Customers =
    {
        new()
        {
            Id = 1,
            Name = "Ahmed Ali",
            Email = "ahmed.ali@example.com",
            IsActive = true,
            LoyaltyTier = LoyaltyTier.Gold
        },
        new()
        {
            Id = 2,
            Name = "Sara Mohamed",
            Email = "sara.mohamed@example.com",
            IsActive = true,
            LoyaltyTier = LoyaltyTier.Silver
        },
        new()
        {
            Id = 3,
            Name = "Omar Hassan",
            Email = "omar.hassan@example.com",
            IsActive = true,
            LoyaltyTier = LoyaltyTier.Bronze
        },
        new()
        {
            Id = 4,
            Name = "Mona Ibrahim",
            Email = "mona.ibrahim@example.com",
            IsActive = false,
            LoyaltyTier = LoyaltyTier.Silver
        },
        new()
        {
            Id = 5,
            Name = "Youssef Adel",
            Email = "youssef.adel@example.com",
            IsActive = true,
            LoyaltyTier = LoyaltyTier.Gold
        },
        new()
        {
            Id = 6,
            Name = "Nour Khaled",
            Email = "nour.khaled@example.com",
            IsActive = true,
            LoyaltyTier = LoyaltyTier.Bronze
        },
        new()
        {
            Id = 7,
            Name = "Karim Samir",
            Email = "karim.samir@example.com",
            IsActive = true,
            LoyaltyTier = LoyaltyTier.Silver
        },
        new()
        {
            Id = 8,
            Name = "Laila Mahmoud",
            Email = "laila.mahmoud@example.com",
            IsActive = true,
            LoyaltyTier = LoyaltyTier.Gold
        },
        new()
        {
            Id = 9,
            Name = "Hassan Tarek",
            Email = "hassan.tarek@example.com",
            IsActive = true,
            LoyaltyTier = LoyaltyTier.Bronze
        },
        new()
        {
            Id = 10,
            Name = "Dina Fathy",
            Email = "dina.fathy@example.com",
            IsActive = true,
            LoyaltyTier = LoyaltyTier.Silver
        }
    };

    private static readonly Product[] Products =
    {
        new()
        {
            Id = 1,
            Name = "Laptop",
            Price = 1200.00m,
            StockQuantity = 10,
            WeightKg = 2.2m
        },
        new()
        {
            Id = 2,
            Name = "Smartphone",
            Price = 800.00m,
            StockQuantity = 15,
            WeightKg = 0.4m
        },
        new()
        {
            Id = 3,
            Name = "Wireless Mouse",
            Price = 25.00m,
            StockQuantity = 2,
            WeightKg = 0.1m
        },
        new()
        {
            Id = 4,
            Name = "Mechanical Keyboard",
            Price = 90.00m,
            StockQuantity = 1,
            WeightKg = 0.9m
        },
        new()
        {
            Id = 5,
            Name = "USB-C Hub",
            Price = 45.00m,
            StockQuantity = 30,
            WeightKg = 0.2m
        },
        new()
        {
            Id = 6,
            Name = "Headphones",
            Price = 150.00m,
            StockQuantity = 12,
            WeightKg = 0.3m
        },
        new()
        {
            Id = 7,
            Name = "27\" Monitor",
            Price = 350.00m,
            StockQuantity = 6,
            WeightKg = 5.0m
        },
        new()
        {
            Id = 8,
            Name = "Webcam",
            Price = 70.00m,
            StockQuantity = 20,
            WeightKg = 0.2m
        }
    };

    private static Coupon[] CreateCoupons(DateTime today)
    {
        return
        [
            new()
            {
                Code = "SAVE10",
                DiscountPercent = 10,
                ExpiryDate = today.AddDays(180)
            },
            new()
            {
                Code = "WELCOME15",
                DiscountPercent = 15,
                ExpiryDate = today.AddDays(90)
            },
            new()
            {
                Code = "FLASH20",
                DiscountPercent = 20,
                ExpiryDate = today.AddDays(7)
            },
            new()
            {
                Code = "OLD20",
                DiscountPercent = 20,
                ExpiryDate = today.AddDays(-30)
            }
        ];
    }

    private static OrderItem CreateOrderItem(
        int productId,
        int quantity)
    {
        return new OrderItem
        {
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = 0m
        };
    }

    private static Order CreateOrder(
        int id,
        int customerId,
        string? couponCode,
        params (int ProductId, int Quantity)[] items)
    {
        return new Order
        {
            Id = id,
            CustomerId = customerId,
            Items = new List<OrderItem>(
            Array.ConvertAll(
                items,
                item => CreateOrderItem(
                    item.ProductId,
            item.Quantity))),
            CouponCode = couponCode,
            SubTotal = 0m,
            Discount = 0m,
            ShippingCost = 0m,
            TotalAmount = 0m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.Now,
            FailureReason = null,
            TransactionId = null
        };
    }

    private static Order[] CreateIncomingOrders()
    {
        return
        [
            CreateOrder(1, 1, "SAVE10", (1, 1), (3, 1)),
            CreateOrder(2, 2, null, (2, 1)),
            CreateOrder(3, 3, "WELCOME15", (5, 2), (8, 1)),
            CreateOrder(4, 4, null, (6, 1)),
            CreateOrder(5, 5, null, (7, 1)),
            CreateOrder(6, 6, "FLASH20", (5, 1)),
            CreateOrder(7, 7, null, (6, 1), (8, 1)),
            CreateOrder(8, 8, "SAVE10", (1, 1)),
            CreateOrder(9, 9, null, (3, 5)),
            CreateOrder(10, 10, null, (4, 1)),
            CreateOrder(11, 1, null, (4, 1)),
            CreateOrder(12, 2, "OLD20", (2, 1)),
            CreateOrder(13, 3, null, (6, 2)),
            CreateOrder(14, 5, "WELCOME15", (8, 2), (5, 1)),
            CreateOrder(15, 99, null, (2, 1)),
            CreateOrder(16, 6, null, (7, 1), (5, 1)),
            CreateOrder(17, 7, "SAVE10", (2, 1)),
            CreateOrder(18, 8, "FAKE50", (6, 1)),
            CreateOrder(19, 9, null, (8, 1)),
            CreateOrder(20, 10, "FLASH20", (1, 1), (6, 1))
        ];
    }

    public async Task ResetAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string dataDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "Data");

        if (Directory.Exists(dataDirectory))
        {
            Directory.Delete(dataDirectory, recursive: true);
        }

        Directory.CreateDirectory(dataDirectory);

        Directory.CreateDirectory(
            Path.Combine(dataDirectory, DataFiles.InvoicesFolder));

        Directory.CreateDirectory(
            Path.Combine(dataDirectory, DataFiles.ReportsFolder));

        cancellationToken.ThrowIfCancellationRequested();

        await _store.WriteAsync(
            DataFiles.Customers,
            Customers,
            cancellationToken);

        PrintSeedResult(DataFiles.Customers, Customers.Length);

        await _store.WriteAsync(
            DataFiles.Products,
            Products,
            cancellationToken);

        PrintSeedResult(DataFiles.Products, Products.Length);

        Coupon[] coupons = CreateCoupons(DateTime.Today);

        await _store.WriteAsync(
            DataFiles.Coupons,
            coupons,
            cancellationToken);

        PrintSeedResult(DataFiles.Coupons, coupons.Length);

        Order[] incomingOrders = CreateIncomingOrders();

        await _store.WriteAsync(
            DataFiles.IncomingOrders,
            incomingOrders,
            cancellationToken);

        PrintSeedResult(
            DataFiles.IncomingOrders,
            incomingOrders.Length);

        await _store.WriteAsync(
            DataFiles.Orders,
            Array.Empty<Order>(),
            cancellationToken);

        PrintSeedResult(DataFiles.Orders, 0);

        await _store.WriteAsync(
            DataFiles.Payments,
            Array.Empty<PaymentTransaction>(),
            cancellationToken);

        PrintSeedResult(DataFiles.Payments, 0);

        await _store.WriteAsync(
            DataFiles.Emails,
            Array.Empty<EmailMessage>(),
            cancellationToken);

        PrintSeedResult(DataFiles.Emails, 0);

        await _store.WriteAsync(
            DataFiles.EmailRetry,
            Array.Empty<object>(),
            cancellationToken);

        PrintSeedResult(DataFiles.EmailRetry, 0);
    }

    private static void PrintSeedResult(
        string fileName,
        int count)
    {
        Console.WriteLine(
            $"[Seeder] {fileName} -> {count} records");
    }
}
