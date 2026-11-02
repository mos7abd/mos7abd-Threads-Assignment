using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Utilities;

namespace OrderProcessing.Services;

public sealed class CustomerDashboardService
{
    private readonly CustomerRepository _customerRepository;
    private readonly OrderRepository _orderRepository;
    private readonly JsonFileStore _store;
    private readonly HashSet<int> _failPaymentsForCustomerIds;

    public CustomerDashboardService(
        CustomerRepository customerRepository,
        OrderRepository orderRepository,
        JsonFileStore store,
        IEnumerable<int>? failPaymentsForCustomerIds = null)
    {
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
        _store = store;

        _failPaymentsForCustomerIds = new HashSet<int>(
            failPaymentsForCustomerIds ?? Enumerable.Empty<int>());
    }

    public async Task<CustomerDashboard> LoadAsync(
        int customerId,
        CancellationToken ct)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Start all four loads before awaiting any of them.
        Task<Customer?> customerTask =
            _customerRepository.GetByIdAsync(customerId, ct);

        Task<List<Order>> ordersTask =
            _orderRepository.GetByCustomerIdAsync(customerId, ct);

        Task<List<PaymentTransaction>> paymentsTask =
            LoadPaymentsAsync(customerId, ct);

        Task<List<EmailMessage>> emailsTask =
            LoadEmailsAsync(customerId, ct);

        Task[] tasks =
        {
            customerTask,
            ordersTask,
            paymentsTask,
            emailsTask
        };

        try
        {
            // One await waits for all four loads.
            await Task.WhenAll(tasks);
        }
        catch
        {
            // Collect every failure instead of reporting only one.
            List<Exception> errors = tasks
                .Where(task => task.IsFaulted)
                .SelectMany(task =>
                    task.Exception!.Flatten().InnerExceptions)
                .ToList();

            if (errors.Count > 0)
                throw new AggregateException(errors);

            throw;
        }
        finally
        {
            stopwatch.Stop();
        }

        Console.WriteLine(
            $"Dashboard loaded in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");

        return new CustomerDashboard
        {
            Customer = customerTask.Result!,
            Orders = ordersTask.Result,
            Payments = paymentsTask.Result,
            Emails = emailsTask.Result
        };
    }

    private async Task<List<PaymentTransaction>> LoadPaymentsAsync(
        int customerId,
        CancellationToken ct)
    {
        Log.Info(
            $"CustomerDashboard -> Loading payments for Customer #{customerId}...");

        await SimulatedLatency.DelayAsync(
            "Dashboard Payments - Open connection",
            200,
            ct);

        int queryDelay =
            SimulatedLatency.RandomMilliseconds(300, 600);

        await SimulatedLatency.DelayAsync(
            "Dashboard Payments - Execute query",
            queryDelay,
            ct);

        if (_failPaymentsForCustomerIds.Contains(customerId))
        {
            throw new TimeoutException("Payments store timeout");
        }

        List<PaymentTransaction> allPayments =
            await _store.ReadAsync<List<PaymentTransaction>>(
                DataFiles.Payments,
                ct)
            ?? new List<PaymentTransaction>();

        List<Order> customerOrders =
            await _store.ReadAsync<List<Order>>(
                DataFiles.Orders,
                ct)
            ?? new List<Order>();

        HashSet<int> customerOrderIds = customerOrders
            .Where(order => order.CustomerId == customerId)
            .Select(order => order.Id)
            .ToHashSet();

        return allPayments
            .Where(payment => customerOrderIds.Contains(payment.OrderId))
            .ToList();
    }

    private async Task<List<EmailMessage>> LoadEmailsAsync(
        int customerId,
        CancellationToken ct)
    {
        Log.Info(
            $"CustomerDashboard -> Loading emails for Customer #{customerId}...");

        await SimulatedLatency.DelayAsync(
            "Dashboard Emails - Open connection",
            200,
            ct);

        int queryDelay =
            SimulatedLatency.RandomMilliseconds(300, 600);

        await SimulatedLatency.DelayAsync(
            "Dashboard Emails - Execute query",
            queryDelay,
            ct);

        List<EmailMessage> allEmails =
            await _store.ReadAsync<List<EmailMessage>>(
                DataFiles.Emails,
                ct)
            ?? new List<EmailMessage>();

        List<Order> customerOrders =
            await _store.ReadAsync<List<Order>>(
                DataFiles.Orders,
                ct)
            ?? new List<Order>();

        HashSet<int> customerOrderIds = customerOrders
            .Where(order => order.CustomerId == customerId)
            .Select(order => order.Id)
            .ToHashSet();

        return allEmails
            .Where(email => customerOrderIds.Contains(email.OrderId))
            .ToList();
    }
}