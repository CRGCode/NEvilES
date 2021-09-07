using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.DataStore;

namespace GTD.SeedData
{
    class Program
    {
        static void Main(string[] args)
        {
            const string connString = "Server=AF-004;Database=ES_GTD;Trusted_Connection=True";

            var container = Startup.Start(connString);

            //SeedData.Initialise(connString, container);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            container.GetRequiredService<ICreateOrWipeDb>().CreateOrWipeDb(new ConnectionString(connString));

            Console.WriteLine($"Finished wiping Db - {stopwatch.Elapsed:g}");
            stopwatch.Restart();
            
            new LoadTest(container, 3, 10, 20).Begin(5, 10);
            Console.WriteLine($"Load test time - {stopwatch.Elapsed:g}");

            Console.WriteLine("Done - Hit any key!");
            Console.ReadKey();
        }
    }
}

    