
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Utilities;

namespace OrderProcessing.Shared.Repositories;

public class CustomerRepository
{
    private readonly JsonFileStore _store;

    public CustomerRepository(JsonFileStore store)
    {
        _store = store;
    }


    public async Task<Customer?> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        Log.Info("CustomerRepository -> Opening connection...");

        await SimulatedLatency.DelayAsync(
            "CustomerRepository - Open connection",
            200,
            ct);

        int queryDelay =
            SimulatedLatency.RandomMilliseconds(300, 600);

        Log.Info(
            $"CustomerRepository -> Executing query for Customer #{id}...");

        await SimulatedLatency.DelayAsync(
            "CustomerRepository - Execute query",
            queryDelay,
            ct);

        List<Customer> customers =
            await _store.ReadAsync<List<Customer>>(
                DataFiles.Customers,
                ct)
            ?? new List<Customer>();

        Customer? customer =
            customers.FirstOrDefault(c => c.Id == id);

        if (customer is not null)
        {
            Log.Success(
                $"CustomerRepository -> Customer #{customer.Id} found ({customer.Name})");
        }
        else
        {
            Log.Info(
                $"CustomerRepository -> Customer #{id} not found");
        }

        return customer;
    }

}
