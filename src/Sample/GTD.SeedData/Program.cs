using System;
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

            container.GetRequiredService<ICreateOrWipeDb>().CreateOrWipeDb(new ConnectionString(connString));

            new LoadTest(container, 3, 10, 20).Begin(10, 10);

            Console.WriteLine("Done - Hit any key!");
            Console.ReadKey();
        }
    }
}

    