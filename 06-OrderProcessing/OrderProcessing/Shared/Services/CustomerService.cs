using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using OrderProcessing.Shared.Utilities;

namespace OrderProcessing.Shared.Services;

public class CustomerService
{
    private readonly CustomerRepository _customerRepository;

    public CustomerService(CustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Customer> GetCustomerAsync(
        int id,
        CancellationToken ct = default)
    {
        Log.Info($"CustomerService -> Loading Customer #{id}...");

        await SimulatedLatency.DelayAsync(
            "CustomerService - Validate request",
            150,
            ct);

        await SimulatedLatency.DelayAsync(
            "CustomerService - Prepare customer lookup",
            SimulatedLatency.RandomMilliseconds(100, 200),
            ct);

        Customer? customer =
            await _customerRepository.GetByIdAsync(id, ct);

        if (customer is null)
        {
            throw new CustomerNotFoundException(
                $"Customer #{id} not found.");
        }

        return customer;
    }

    // Sync version
    public Customer GetCustomer(int id)
    {
        Log.Info($"CustomerService -> Loading Customer #{id}...");

        Thread.Sleep(150);

        Log.Info(
            "[CustomerService - Validate request] Delay completed");

        Thread.Sleep(
            SimulatedLatency.RandomMilliseconds(100, 200));

        Log.Info(
            "[CustomerService - Prepare customer lookup] Delay completed");

        Customer? customer =
            _customerRepository.GetById(id);

        if (customer is null)
        {
            throw new CustomerNotFoundException(
                $"Customer #{id} not found.");
        }

        return customer;
    }
}