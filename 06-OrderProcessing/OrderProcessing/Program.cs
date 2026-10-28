using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Repositories;
using OrderProcessing.Shared.Services;
using OrderProcessing.Shared.Storage;
using System.Diagnostics;
using System.Text.Json;



namespace OrderProcessing
{

    internal class Program
    {

        static async Task Main(string[] args)
        {

            #region Phase1Runner

            //// 1. Shared storage
            //var store = new JsonFileStore();

            //// 2. Data seeder
            //var dataSeeder = new DataSeeder(store);

            //// 3. Repositories
            //var incomingOrderRepository =
            //    new IncomingOrderRepository(store);

            //var orderRepository =
            //    new OrderRepository(store);

            //var customerRepository =
            //    new CustomerRepository(store);

            //var productRepository =
            //    new ProductRepository(store);

            //var couponRepository =
            //    new CouponRepository(store);

            //var syncCustomerRepository =
            //    new SyncCustomerRepository(store);

            //var syncProductRepository =
            //    new SyncProductRepository(store);

            //var syncCouponRepository =
            //    new SyncCouponRepository(store);

            //// 4. Validators
            //var orderValidator = new OrderValidator(
            //    customerRepository,
            //    productRepository,
            //    couponRepository,
            //    orderRepository);

            //var syncOrderValidator = new SyncOrderValidator(
            //    syncCustomerRepository,
            //    syncProductRepository,
            //    syncCouponRepository);

            //// 5. Phase 1 runner
            //var runner = new Phase1Runner(
            //    dataSeeder,
            //    incomingOrderRepository,
            //    syncOrderValidator,
            //    orderValidator,
            //    orderRepository);

            //// 6. Cancellation setup
            //using var cts = new CancellationTokenSource();

            //ConsoleCancelEventHandler cancelHandler = (_, e) =>
            //{
            //    e.Cancel = true;
            //    cts.Cancel();
            //};

            //Console.CancelKeyPress += cancelHandler;

            //try
            //{
            //    int cancelAfterIndex =
            //        Array.IndexOf(args, "--cancel-after");

            //    if (cancelAfterIndex >= 0)
            //    {
            //        if (cancelAfterIndex + 1 >= args.Length ||
            //            !int.TryParse(
            //                args[cancelAfterIndex + 1],
            //                out int delayMs) ||
            //            delayMs < 0)
            //        {
            //            Console.WriteLine(
            //                "Usage: --cancel-after <milliseconds>");
            //            return;
            //        }

            //        cts.CancelAfter(delayMs);
            //    }

            //    await runner.RunAsync(cts.Token);
            //}
            //catch (OperationCanceledException)
            //    when (cts.IsCancellationRequested)
            //{
            //    Console.WriteLine("\nPhase 1 cancellation requested.");
            //}
            //finally
            //{
            //    Console.CancelKeyPress -= cancelHandler;
            //}

            #endregion



            #region phase2Runner


            var store = new JsonFileStore();

            var dataSeeder = new DataSeeder(store);

            var incomingOrderRepository =
                new IncomingOrderRepository(store);

            var productRepository =
                new ProductRepository(store);

            var customerRepository =
                new CustomerRepository(store);

            var couponRepository =
                new CouponRepository(store);

            var orderRepository =
                new OrderRepository(store);

            var orderValidator = new OrderValidator(
                customerRepository,
                productRepository,
                couponRepository,
                orderRepository);

            var pricingService = new PricingService(
                store,
                productRepository,
                customerRepository,
                couponRepository,
                new[] { 7 });

            var orderPricer = new OrderPricer(pricingService);

            var raceConditionDemo = new RaceConditionDemo(
                store,
                productRepository);

            // Create the invoice generator.
            var invoiceGenerator = new InvoiceGenerator();

            // Inject it into the order processor.
            var orderProcessor = new OrderProcessorV2(
                productRepository,
                orderRepository,
                pricingService,
                invoiceGenerator);

            var phase2Runner = new Phase2Runner(
                dataSeeder,
                incomingOrderRepository,
                orderValidator,
                orderRepository,
                productRepository,
                raceConditionDemo,
                orderPricer,
                orderProcessor);

            using var cts = new CancellationTokenSource();

            ConsoleCancelEventHandler cancelHandler = (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            Console.CancelKeyPress += cancelHandler;

            try
            {
                int cancelAfterIndex =
                    Array.IndexOf(args, "--cancel-after");

                if (cancelAfterIndex >= 0)
                {
                    if (cancelAfterIndex + 1 >= args.Length ||
                        !int.TryParse(
                            args[cancelAfterIndex + 1],
                            out int delayMs) ||
                        delayMs < 0)
                    {
                        Console.WriteLine(
                            "Usage: --cancel-after <milliseconds>");
                        return;
                    }

                    cts.CancelAfter(delayMs);
                }

                await phase2Runner.RunAsync(cts.Token);
            }
            catch (OperationCanceledException)
                when (cts.IsCancellationRequested)
            {
                Console.WriteLine("\nPhase 2 cancellation requested.");
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
            }


            #endregion

            #region MyRegion
            var reportGenerator = new ReportGenerator(
                orderRepository,
                productRepository);

            Task<Report> reportTask = reportGenerator.GenerateAsync();

            Console.WriteLine("Generating report...");

            Report report = await reportTask;

            Console.WriteLine(
                JsonSerializer.Serialize(
                    report,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
            #endregion






        }
    }

}
