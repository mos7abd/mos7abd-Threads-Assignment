
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using OrderProcessing.Shared.Utilities;

namespace OrderProcessing.Shared.Repositories;

public class SyncCustomerRepository
{
    private readonly JsonFileStore _store;

    public SyncCustomerRepository(JsonFileStore store)
    {
        _store = store;
    }

    public Customer? GetById(int id)
    {
        Log.Info(
            $"SyncCustomerRepository -> Opening connection to customers...");

        Thread.Sleep(200);

        int delay =
            SimulatedLatency.RandomMilliseconds(300, 600);

        Log.Info(
            $"SyncCustomerRepository -> Query delay: {delay}ms");

        Thread.Sleep(delay);

        List<Customer> customers =
            _store.Read<List<Customer>>(DataFiles.Customers)
            ?? new List<Customer>();

        Customer? customer =
            customers.FirstOrDefault(c => c.Id == id);

        if (customer is null)
        {
            Log.Error(
                $"SyncCustomerRepository -> Customer #{id} not found.");

            return null;
        }

        Log.Success(
            $"SyncCustomerRepository -> Customer #{id} found.");

        return customer;
    }
}
