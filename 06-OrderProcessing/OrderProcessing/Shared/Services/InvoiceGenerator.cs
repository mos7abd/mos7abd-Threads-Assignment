
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Utilities;

namespace OrderProcessing.Shared.Services
{
    public class InvoiceGenerator
    {
        public async Task GenerateAsync(
            Order order,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(order);

            cancellationToken.ThrowIfCancellationRequested();

            // Step 1: Simulate invoice preparation latency.
            await SimulatedLatency.DelayAsync(
                $"InvoiceGenerator - Order {order.Id} - Invoice preparation",
                200,
                cancellationToken);

            // Step 2: Simulate invoice rendering latency.
            await SimulatedLatency.DelayAsync(
                $"InvoiceGenerator - Order {order.Id} - Invoice rendering",
                300,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            Log.Info(
                $"InvoiceGenerator -> Order {order.Id}: CPU work started.");

            // CPU-bound work: simulate rendering an invoice.
            long checksum = await Task.Run(() =>
            {
                long result = 0;

                for (int i = 0; i < 20_000_000; i++)
                {
                    if (i % 100_000 == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    result += (long)i * (i % 97);
                }

                return result;
            }, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            Log.Info(
                $"InvoiceGenerator -> Order {order.Id}: CPU work completed.");

            string directory = Path.Combine("Data", "invoices");
            Directory.CreateDirectory(directory);

            string filePath = Path.Combine(
                directory,
                $"invoice_{order.Id}.txt");

            string content =
                $"Invoice #{order.Id}{Environment.NewLine}" +
                $"Customer ID: {order.CustomerId}{Environment.NewLine}" +
                $"Subtotal: {order.SubTotal:F2}{Environment.NewLine}" +
                $"Discount: {order.Discount:F2}{Environment.NewLine}" +
                $"Shipping: {order.ShippingCost:F2}{Environment.NewLine}" +
                $"Total: {order.TotalAmount:F2}{Environment.NewLine}" +
                $"Status: {order.Status}{Environment.NewLine}" +
                $"Checksum: {checksum}{Environment.NewLine}";

            cancellationToken.ThrowIfCancellationRequested();

            Log.Info(
                $"InvoiceGenerator -> Order {order.Id}: Writing invoice.");

            await File.WriteAllTextAsync(
                filePath,
                content,
                cancellationToken);

            Log.Success(
                $"InvoiceGenerator -> Order {order.Id}: Invoice generated at {filePath}");
        }
    }
}
