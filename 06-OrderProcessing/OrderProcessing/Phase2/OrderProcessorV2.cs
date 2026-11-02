
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using OrderProcessing.Shared.Services;

namespace OrderProcessing.Phase2;

public class OrderProcessorV2
{
    private readonly ProductRepository _productRepository;
    private readonly OrderRepository _orderRepository;
    private readonly PricingService _pricingService;
    private readonly InvoiceGenerator _invoiceGenerator;

    public OrderProcessorV2(
        ProductRepository productRepository,
        OrderRepository orderRepository,
        PricingService pricingService,
        InvoiceGenerator invoiceGenerator)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _pricingService = pricingService;
        _invoiceGenerator = invoiceGenerator;
    }

    public async Task<bool> ProcessValidatedOrderAsync(
        Order order,
        CancellationToken ct = default)
    {
        var reservedItems = new List<OrderItem>();

        foreach (var item in order.Items)
        {
            bool reserved = await _productRepository.ReserveStockAsync(
                item.ProductId,
                item.Quantity,
                ct);

            if (!reserved)
            {
                foreach (var reservedItem in reservedItems)
                {
                    await _productRepository.ReleaseStockAsync(
                        reservedItem.ProductId,
                        reservedItem.Quantity,
                        ct);
                }

                var product = await _productRepository.GetByIdAsync(
                    item.ProductId,
                    ct);

                string productName =
                    product?.Name ?? $"Product #{item.ProductId}";

                order.Status = OrderStatus.Rejected;
                order.FailureReason =
                    $"Insufficient stock for '{productName}'";

                await _orderRepository.SaveAsync(order, ct);

                Log.Error(
                    $"Order #{order.Id} rejected: {order.FailureReason}");

                return false;
            }

            reservedItems.Add(item);
        }


        await _pricingService.PriceOrderAsync(order, ct);

        await _invoiceGenerator.GenerateAsync(order, ct);

        order.Status = OrderStatus.Completed;
        order.FailureReason = null;

        await _orderRepository.SaveAsync(order, ct);

        Log.Success(
            $"Order #{order.Id} completed successfully.");

        return true;
    }
}
