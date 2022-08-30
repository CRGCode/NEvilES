using System;
using System.Diagnostics;
using System.Linq;
using GTD.ReadModel;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.DataStore;
using NEvilES.Abstractions.Pipeline;

namespace GTD.SeedData
{
    class Program
    {
        static void Main(string[] args)
        {
            //const string connString = "Server=AF-004;Database=ES_GTD;Trusted_Connection=True";
            //var container = SQLStartup.Start(connString);

            const string connString = "Host=localhost;Username=postgres;Password=password;Database=gtd";
            var container = PostgresStartup.RegisterServices(connString);
            //SeedData.Initialise(connString, container);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var createOrWipeDb = container.GetRequiredService<ICreateOrWipeDb>();
            createOrWipeDb.CreateOrWipeDb(new ConnectionString(connString));

            Console.WriteLine($"Finished wiping Db - {stopwatch.Elapsed:g}");
            stopwatch.Restart();
            
            new LoadTest(container, 3, 10, 20).Begin(5, 10);
            Console.WriteLine($"Load test time - {stopwatch.Elapsed:g}");

            var reader = container.GetRequiredService<IReadFromReadModel<Guid>>();

            var text = "R1";
            var requests = reader.Query<Request>(r =>
                    r.Description.Contains(text) ||
                    r.ShortName.Contains(text))
                .OrderByDescending(x => x.Priority)
                .ToList();

            if (requests.Count != 20)
            {
                Console.WriteLine($"Results not correct, using Search text of '{text}' count = {requests.Count}");
            }
            Console.WriteLine("Done - Hit any key!");
            Console.ReadKey();
        }
    }
}

    