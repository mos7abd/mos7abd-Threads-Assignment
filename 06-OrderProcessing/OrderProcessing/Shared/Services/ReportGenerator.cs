
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using OrderProcessing.Shared.Utilities;

namespace OrderProcessing.Shared.Services;

public class ReportGenerator
{
    private readonly OrderRepository _orderRepository;
    private readonly ProductRepository _productRepository;

    public ReportGenerator(
        OrderRepository orderRepository,
        ProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }

    public async Task<Report> GenerateAsync(
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await SimulatedLatency.DelayAsync(
            "ReportGenerator - Prepare report",
            SimulatedLatency.RandomMilliseconds(200, 400),
            ct);

        Log.Info("ReportGenerator -> Loading orders...");

        var orders = await _orderRepository.GetAllAsync(ct);

        ct.ThrowIfCancellationRequested();

        var completedOrders = orders
            .Where(o => o.Status == OrderStatus.Completed)
            .ToList();

        decimal totalRevenue = completedOrders
            .Sum(o => o.TotalAmount);

        var productQuantities = completedOrders
            .SelectMany(o => o.Items)
            .GroupBy(item => item.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            })
            .OrderByDescending(product => product.Quantity)
            .ThenBy(product => product.ProductId)
            .ToList();

        string topSellingProduct = string.Empty;
        int topSellingQuantity = 0;

        if (productQuantities.Count > 0)
        {
            var topProduct = productQuantities[0];

            var product = await _productRepository.GetByIdAsync(
                topProduct.ProductId,
                ct);

            topSellingProduct = product?.Name ?? "Unknown";
            topSellingQuantity = topProduct.Quantity;
        }

        var report = new Report
        {
            TotalOrders = orders.Count,

            CompletedCount = orders.Count(
                o => o.Status == OrderStatus.Completed),

            RejectedCount = orders.Count(
                o => o.Status == OrderStatus.Rejected),

            FailedCount = orders.Count(
                o => o.Status == OrderStatus.Failed),

            PaymentFailedCount = orders.Count(
                o => o.Status == OrderStatus.PaymentFailed),

            TotalRevenue = totalRevenue,

            AverageOrderValue = completedOrders.Count == 0
                ? 0
                : totalRevenue / completedOrders.Count,

            TopSellingProduct = topSellingProduct,
            TopSellingQuantity = topSellingQuantity,

            GeneratedAt = DateTime.Now
        };

        await SimulatedLatency.DelayAsync(
            "ReportGenerator - Prepare report file",
            SimulatedLatency.RandomMilliseconds(200, 400),
            ct);

        ct.ThrowIfCancellationRequested();

        string directory = Path.Combine("Data", "reports");
        Directory.CreateDirectory(directory);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        string filePath = Path.Combine(
            directory,
            $"report_{timestamp}.json");

        string json = JsonSerializer.Serialize(
            report,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        ct.ThrowIfCancellationRequested();

        await File.WriteAllTextAsync(filePath, json, ct);

        Log.Success(
            $"ReportGenerator -> Report saved to {filePath}");

        return report;
    }
}
