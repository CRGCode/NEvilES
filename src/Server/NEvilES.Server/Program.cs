using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration.Json;
using NEvilES.Server;
using NEvilES.Server.ClientApi;


var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, builder) =>
    {
        builder.AddJsonFile("appsettings.json");
    })
    .ConfigureServices(services =>
    {
		services.AddEventServer();
        services.AddEventServerClient();
        services.AddHostedService<EventServer>();
        services.AddHostedService<SampleClient>();

    })
    .UseEnvironment("Debug")
    .Build();

await host.RunAsync();


	/*
	internal class Program
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		public static void Main(string[] args)
		{
			var address = ConfigurationManager.AppSettings["Address"];
			if(string.IsNullOrEmpty(address))
			{
				// DNS lookup method
				//var hostName = Dns.GetHostEntry(Dns.GetHostName());
				//address = hostName.AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork).ToString();

				// non DNS method
				address = Global.GetIPAddress();
			}

			var connectionString = ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString;
			var basePort = ConfigurationManager.AppSettings["BasePort"];
			var csb = new SqlConnectionStringBuilder(connectionString);
			
			var title = string.Format("ES Server[{2}] - {0} Port - {1}", csb.InitialCatalog, basePort, Assembly.GetExecutingAssembly().GetName().Version);


			var resend = string.IsNullOrEmpty(ConfigurationManager.AppSettings["Resend"])
				? 0
				: int.Parse(ConfigurationManager.AppSettings["Resend"]);

			var host = HostFactory.New(x =>
			{
				x.SetServiceName("CRG.ES.Server");
				x.SetDescription("CRG Event Store Server");
				x.SetDisplayName(title);
				x.Service<EventServer>(s =>
				{
					s.ConstructUsing(name => new EventServer(connectionString,address,int.Parse(basePort),resend));
					s.WhenStarted(es =>
					{
						logger.Info("Starting - {0} Port - {1}", csb.InitialCatalog, basePort);
						es.Start();
					});
					s.WhenStopped(es =>
					{
						logger.Warn("Stopping - {0}", csb.InitialCatalog, basePort);
						es.Stop();
					});
				});
				x.StartAutomatically();
				x.UseNLog();
			});
			var exitcode = host.Run();

			//  TODO - Trying to detect the type of host and if there's any errors and pause - But it's not working!
			if (host.GetType().Name.Contains("Console") && exitcode == TopshelfExitCode.UnhandledServiceException)
			{
				Console.WriteLine("Paused due to exception");
				while (true)
				{
					
				}
			}
		}
	}
}
*/