using OrderProcessing.Interfaces;
using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Phase4;
using OrderProcessing.Services;
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
            // var store = new JsonFileStore();

            //// 2. Data seeder
            // var dataSeeder = new DataSeeder(store);

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


            //var store = new JsonFileStore();

            //var dataSeeder = new DataSeeder(store);

            //var incomingOrderRepository =
            //    new IncomingOrderRepository(store);

            //var productRepository =
            //    new ProductRepository(store);

            //var customerRepository =
            //    new CustomerRepository(store);

            //var couponRepository =
            //    new CouponRepository(store);

            //var orderRepository =
            //    new OrderRepository(store);

            //var orderValidator = new OrderValidator(
            //    customerRepository,
            //    productRepository,
            //    couponRepository,
            //    orderRepository);

            //var pricingService = new PricingService(
            //    store,
            //    productRepository,
            //    customerRepository,
            //    couponRepository,
            //    new[] { 7 });

            //var orderPricer = new OrderPricer(pricingService);

            //var raceConditionDemo = new RaceConditionDemo(
            //    store,
            //    productRepository);

            // Create the invoice generator.
            //var invoiceGenerator = new InvoiceGenerator();

            // Inject it into the order processor.
            //var orderProcessor = new OrderProcessorV2(
            //    productRepository,
            //    orderRepository,
            //    pricingService,
            //    invoiceGenerator);

            //var phase2Runner = new Phase2Runner(
            //    dataSeeder,
            //    incomingOrderRepository,
            //    orderValidator,
            //    orderRepository,
            //    productRepository,
            //    raceConditionDemo,
            //    orderPricer,
            //    orderProcessor);

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

            //    await phase2Runner.RunAsync(cts.Token);
            //}
            //catch (OperationCanceledException)
            //    when (cts.IsCancellationRequested)
            //{
            //    Console.WriteLine("\nPhase 2 cancellation requested.");
            //}
            //finally
            //{
            //    Console.CancelKeyPress -= cancelHandler;
            //}


            #endregion

            #region OrderProcessingRunner


            //var store = new JsonFileStore();

            //// 1. Create repositories.
            //var customerRepository = new CustomerRepository(store);
            //var productRepository = new ProductRepository(store);
            //var couponRepository = new CouponRepository(store);

            //// 2. Create services.
            //var customerService = new CustomerService(customerRepository);
            //var inventoryService = new InventoryService(store);

            //var pricingService = new PricingService(
            //    store,
            //    productRepository,
            //    customerRepository,
            //    couponRepository,
            //    Array.Empty<int>());

            //// 3. Configure payment scenarios.
            //var paymentGateway = new PaymentGateway(
            //    store,
            //    timeoutOrderIds: new[] { 5 },
            //    declineOrderIds: new[] { 8 });

            //// 4. Configure email retry scenarios.
            //var emailService = new EmailService(
            //    store,
            //    failAttempts: new Dictionary<int, int>
            //    {
            //        [3] = 2,  // Fails twice, succeeds on attempt 3.
            //        [17] = 4  // Fails four times.
            //    });

            //// 5. Configure database save failure.
            //var orderRepository = new OrderRepository(
            //    store,
            //    failSaveForOrderIds: new[] { 13 });

            //// 6. Create the order processor.
            //var processor = new OrderProcessor(
            //    customerService,
            //    inventoryService,
            //    pricingService,
            //    paymentGateway,
            //    orderRepository,
            //    emailService,
            //    productRepository,
            //    store);

            //// 7. Create the data seeder.
            //var seeder = new DataSeeder(store);

            //// 8. Create the runner.
            //var runner = new OrderProcessingRunner(
            //    seeder,
            //    store,
            //    processor);

            //// 9. Read command-line arguments.
            //int? cancelAfterMs = null;

            //int cancelIndex = Array.IndexOf(args, "--cancel-after");

            //if (cancelIndex >= 0)
            //{
            //    if (cancelIndex + 1 >= args.Length ||
            //        !int.TryParse(args[cancelIndex + 1], out int milliseconds) ||
            //        milliseconds <= 0)
            //    {
            //        Console.WriteLine(
            //            "Invalid argument. Usage: --cancel-after <milliseconds>");

            //        return;
            //    }

            //    cancelAfterMs = milliseconds;
            //}

            //// 10. Run Phase 3.
            //try
            //{
            //    await runner.RunAsync(cancelAfterMs);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Application error: {ex.Message}");
            //}
            #endregion

            #region SyncVsAsyncRunner

            //var store = new JsonFileStore();

            //var customerRepository = new CustomerRepository(store);
            //var productRepository = new ProductRepository(store);
            //var couponRepository = new CouponRepository(store);

            //var customerService = new CustomerService(customerRepository);
            //var inventoryService = new InventoryService(store);

            //// Failures disabled for comparison.
            //var pricingService = new PricingService(
            //    store,
            //    productRepository,
            //    customerRepository,
            //    couponRepository,
            //    Array.Empty<int>());

            //var paymentGateway = new PaymentGateway(
            //    store,
            //    timeoutOrderIds: Array.Empty<int>(),
            //    declineOrderIds: Array.Empty<int>(),
            //    failuresEnabled: false);

            //var emailService = new EmailService(
            //    store,
            //    failAttempts: new Dictionary<int, int>(),
            //    failuresEnabled: false);

            //var orderRepository = new OrderRepository(
            //    store,
            //    failSaveForOrderIds: Array.Empty<int>());

            //var asyncProcessor = new OrderProcessor(
            //    customerService,
            //    inventoryService,
            //    pricingService,
            //    paymentGateway,
            //    orderRepository,
            //    emailService,
            //    productRepository,
            //    store);

            //var syncProcessor = new SyncOrderProcessor(
            //    customerService,
            //    inventoryService,
            //    pricingService,
            //    paymentGateway,
            //    orderRepository,
            //    emailService,
            //    productRepository,
            //    store);

            //var seeder = new DataSeeder(store);

            //var runner = new SyncVsAsyncRunner(
            //    seeder,
            //    store,
            //    syncProcessor,
            //    asyncProcessor);

            //await runner.RunAsync();
            #endregion

            #region Fire-and-forget investigation
            //var store = new JsonFileStore();

            //var customerRepository = new CustomerRepository(store);
            //var productRepository = new ProductRepository(store);
            //var couponRepository = new CouponRepository(store);

            //var customerService = new CustomerService(customerRepository);
            //var inventoryService = new InventoryService(store);

            //var pricingService = new PricingService(
            //    store,
            //    productRepository,
            //    customerRepository,
            //    couponRepository,
            //    Array.Empty<int>());

            //var paymentGateway = new PaymentGateway(
            //    store,
            //    failuresEnabled: false);

            //var emailService = new EmailService(
            //    store,
            //    failuresEnabled: false);

            //var orderRepository = new OrderRepository(
            //    store,
            //    failSaveForOrderIds: Array.Empty<int>());

            //var processor = new OrderProcessor(
            //    customerService,
            //    inventoryService,
            //    pricingService,
            //    paymentGateway,
            //    orderRepository,
            //    emailService,
            //    productRepository,
            //    store);


            //Customer customer = new()
            //{
            //    Id = 1,
            //    Name = "Ahmed Ali",
            //    Email = "ahmed.ali@example.com",
            //    IsActive = true
            //};

            //Order order = new()
            //{
            //    Id = 101,
            //    CustomerId = customer.Id,
            //    Status = OrderStatus.Pending
            //};

            //Console.WriteLine("===== BEFORE FIX =====");

            //await processor.ProcessOrderFireAndForgetAsync(order, customer);

            //Console.WriteLine("===== AFTER FIX =====");

            //await processor.ProcessOrderWithAwaitAsync(order, customer);

            #endregion


            #region Phase 4 – Task 10.1: Live Checkout Progress
            //var store = new JsonFileStore();

            //// 1. Create repositories.
            //var customerRepository = new CustomerRepository(store);
            //var productRepository = new ProductRepository(store);
            //var couponRepository = new CouponRepository(store);

            //// 2. Create services.
            //var customerService = new CustomerService(customerRepository);
            //var inventoryService = new InventoryService(store);

            //var pricingService = new PricingService(
            //    store,
            //    productRepository,
            //    customerRepository,
            //    couponRepository,
            //    Array.Empty<int>());

            //// 3. Configure payment scenarios.
            //var paymentGateway = new PaymentGateway(
            //    store,
            //    timeoutOrderIds: new[] { 5 },
            //    declineOrderIds: new[] { 8 });

            //// 4. Configure email retry scenarios.
            //var emailService = new EmailService(
            //    store,
            //    failAttempts: new Dictionary<int, int>
            //    {
            //        [3] = 2,
            //        [17] = 4
            //    });

            //// 5. Configure database save failure.
            //var orderRepository = new OrderRepository(
            //    store,
            //    failSaveForOrderIds: new[] { 13 });

            //// 6. Create the order processor.
            //var processor = new OrderProcessor(
            //    customerService,
            //    inventoryService,
            //    pricingService,
            //    paymentGateway,
            //    orderRepository,
            //    emailService,
            //    productRepository,
            //    store);

            //// 7. Create the data seeder.
            //var seeder = new DataSeeder(store);

            //// 8. Create the live checkout runner.
            //var runner = new LiveCheckoutRunner(
            //    seeder,
            //    processor,
            //    store);

            //// 9. Configure cancellation.
            //using var cts = new CancellationTokenSource();

            //ConsoleCancelEventHandler checkoutHandler = (_, e) =>
            //{
            //    e.Cancel = true;

            //    Console.WriteLine(
            //        "\nCancellation requested. Stopping checkout...");

            //    cts.Cancel();
            //};

            //try
            //{
            //    Console.CancelKeyPress += checkoutHandler;

            //    await runner.RunAsync(cts.Token);
            //}
            //catch (OperationCanceledException)
            //{
            //    Console.WriteLine("Checkout was cancelled.");
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Application error: {ex.Message}");
            //}
            //finally
            //{
            //    Console.CancelKeyPress -= checkoutHandler;
            //}
            #endregion

            #region Phase 4 - Task 10.2 — Customer dashboard
            //var dashboardService = new CustomerDashboardService(
            //        customerRepository,
            //        orderRepository,
            //        store,
            //        failPaymentsForCustomerIds: new[] { 99 });

            //try
            //{
            //    CustomerDashboard dashboard =
            //        await dashboardService.LoadAsync(3, CancellationToken.None);

            //    Console.WriteLine(
            //        $"Customer #{dashboard.Customer.Id} {dashboard.Customer.Name}");

            //    Console.WriteLine(
            //        $"Orders: {dashboard.Orders.Count}");

            //    Console.WriteLine(
            //        $"Payments: {dashboard.Payments.Count}");

            //    Console.WriteLine(
            //        $"Emails: {dashboard.Emails.Count}");
            //}
            //catch (AggregateException ex)
            //{
            //    foreach (Exception error in ex.InnerExceptions)
            //        Console.WriteLine(error.Message);
            //}

            //try
            //{
            //    await dashboardService.LoadAsync(99, CancellationToken.None);
            //}
            //catch (AggregateException ex)
            //{
            //    Console.WriteLine("Dashboard loading failed:");

            //    foreach (Exception error in ex.InnerExceptions)
            //        Console.WriteLine(error.Message);
            //}
            #endregion

            #region Phase 4 - Task 10.3 — Fastest Shipping Quote

            // 1. Create the three shipping carriers.
            //    var fastShip = new FastShipCarrier(store);

            //    var cairoExpress = new CairoExpressCarrier(store);

            //    var nileCourier = new NileCourierCarrier(store);

            //    // 2. Create the shipping quote service.
            //    var shippingQuoteService = new ShippingQuoteService(
            //        new IShippingCarrier[]
            //        {
            //fastShip,
            //cairoExpress,
            //nileCourier
            //        });

            //    // 3. Load existing orders.
            //    try
            //    {
            //        List<Order> orders =
            //            await store.ReadAsync<List<Order>>(
            //                DataFiles.Orders,
            //                CancellationToken.None)
            //            ?? new List<Order>();

            //        // 4. Run shipping quotes for orders #1 to #5.
            //        foreach (var order in orders.Where(o => o.Id >= 1 && o.Id <= 5)
            //                                    .OrderBy(o => o.Id))
            //        {
            //            Console.WriteLine(
            //                $"\n--- Shipping Quote: Order #{order.Id} ---");

            //            try
            //            {
            //                ShippingQuote quote =
            //                    await shippingQuoteService.GetFastestQuoteAsync(
            //                        order,
            //                        CancellationToken.None);

            //                Console.WriteLine($"Order: #{order.Id}");
            //                Console.WriteLine($"Winner: {quote.Carrier}");
            //                Console.WriteLine($"Price: {quote.Price:F2}");
            //                Console.WriteLine($"Time: {quote.ElapsedMs} ms");
            //                Console.WriteLine(
            //                    $"Cancelled: {string.Join(", ", quote.CancelledCarriers)}");
            //            }
            //            catch (ShippingProviderUnavailableException ex)
            //            {
            //                Console.WriteLine($"Order #{order.Id} failed: {ex.Message}");
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(
            //            $"Shipping quote test error: {ex.Message}");
            //    }

            //    // R6: Test when all carriers fail for Order #6.
            //    Console.WriteLine("\n--- R6: All Carriers Failed ---");

            //    var failingFastShip = new FastShipCarrier(
            //        store,
            //        failForOrderIds: new[] { 6 });

            //    var failingCairoExpress = new CairoExpressCarrier(
            //        store,
            //        failForOrderIds: new[] { 6 });

            //    var failingNileCourier = new NileCourierCarrier(
            //        store,
            //        failForOrderIds: new[] { 6 });

            //    var failingShippingService = new ShippingQuoteService(
            //                                    new IShippingCarrier[]
            //                                    {
            //                                        failingFastShip,
            //                                        failingCairoExpress,
            //                                        failingNileCourier
            //                                    });

            //    var allOrders = await store.ReadAsync<List<Order>>(
            //                     DataFiles.Orders,
            //                     CancellationToken.None);

            //    var order6 = allOrders?.FirstOrDefault(o => o.Id == 6);

            //    if (order6 is null)
            //    {
            //        Console.WriteLine("Order #6 was not found.");
            //        return;
            //    }

            //    try
            //    {
            //        ShippingQuote quote =
            //            await failingShippingService.GetFastestQuoteAsync(
            //                order6,
            //                CancellationToken.None);

            //        Console.WriteLine($"Unexpected result: {quote.Carrier}");
            //    }
            //    catch (ShippingProviderUnavailableException ex)
            //    {
            //        Console.WriteLine($"Order #6 failed as expected: {ex.Message}");
            //    }


            //    // R7: Test caller cancellation using Ctrl+C.
            //    Console.WriteLine("\n--- R7: Caller Cancellation ---");

            //    var order1 = allOrders?.FirstOrDefault(o => o.Id == 1);

            //    if (order1 is null)
            //    {
            //        Console.WriteLine("Order #1 was not found.");
            //        return;
            //    }

            //    using var cancellationCts = new CancellationTokenSource();

            //    ConsoleCancelEventHandler cancelHandler = (_, e) =>
            //    {
            //        e.Cancel = true;

            //        if (!cancellationCts.IsCancellationRequested)
            //            cancellationCts.Cancel();
            //    };

            //    try
            //    {
            //        Console.CancelKeyPress += cancelHandler;

            //        Console.WriteLine("Get ready. Shipping starts in 5 seconds...");
            //        await Task.Delay(5000);

            //        Console.WriteLine("Shipping quote started! Press Ctrl+C NOW!");

            //        await shippingQuoteService.GetFastestQuoteAsync(
            //            order1,
            //            cancellationCts.Token);
            //    }
            //    catch (OperationCanceledException)
            //    {
            //        Console.WriteLine("Shipping quote cancelled by the caller.");
            //    }
            //    finally
            //    {
            //        Console.CancelKeyPress -= cancelHandler;
            //    }


            #endregion

            #region #region Task 10.4 - Fraud Scoring and Email Re-send

            // var task10_4 = new Task10_4_FraudScoringAndEmailResend();

            // using var fraudScoringCts = new CancellationTokenSource();

            //// fraudScoringCts.CancelAfter(TimeSpan.FromMilliseconds(100));

            // try
            // {
            //     task10_4.RunFraudScoring(fraudScoringCts.Token);
            // }
            // catch (OperationCanceledException)
            // {
            //     Console.WriteLine("Fraud Scoring was cancelled.");
            // }

            // Task 10.4 - Email Re-send

            var task10_4 = new Task10_4_FraudScoringAndEmailResend();

            using var emailCts = new CancellationTokenSource();

            try
            {
                await task10_4.RunEmailResendAsync(emailCts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Email Re-send was cancelled.");
            }


            #endregion







        }



    }
}










