using OrderProcessing.Phase1;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using OrderProcessing.Shared.Services;
using OrderProcessing.Shared.Storage;



namespace OrderProcessing
{

    internal class Program
    {

        static async Task Main(string[] args)
        {
            #region Phase1Runner
            // 1. Shared storage
            var store = new JsonFileStore();

            // 2. Data seeder
            var dataSeeder = new DataSeeder(store);

            // 3. Repositories
            var incomingOrderRepository =
                new IncomingOrderRepository(store);

            var orderRepository =
                new OrderRepository(store);

            var customerRepository =
                new CustomerRepository(store);

            var productRepository =
                new ProductRepository(store);

            var couponRepository =
                new CouponRepository(store);

            var syncCustomerRepository =
                new SyncCustomerRepository(store);

            var syncProductRepository =
                new SyncProductRepository(store);

            var syncCouponRepository =
                new SyncCouponRepository(store);

            // 4. Validators
            var orderValidator = new OrderValidator(
                customerRepository,
                productRepository,
                couponRepository,
                orderRepository);

            var syncOrderValidator = new SyncOrderValidator(
                syncCustomerRepository,
                syncProductRepository,
                syncCouponRepository);

            // 5. Phase 1 runner
            var runner = new Phase1Runner(
                dataSeeder,
                incomingOrderRepository,
                syncOrderValidator,
                orderValidator,
                orderRepository);

            // 6. Run
            await runner.RunAsync();
            #endregion

        }
        }

    }
