using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;



namespace OrderProcessing
{

    internal class Program
    {

        static async Task Main(string[] args)
        {


            var store = new JsonFileStore();
            var seeder = new DataSeeder(store);

            await seeder.ResetAsync();

            Console.WriteLine("Data seeding completed successfully.");

        }
    }
}
