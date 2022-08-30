using System;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Server.Abstractions;
using Newtonsoft.Json;

namespace NEvilES.Server.ClientApi
{
	public class Client : IEventServer
	{
		private readonly string zmqReadConnectionString;  // "tcp://127.0.0.1:5454"
		private readonly string zmqWriteConnectionString; // "tcp://127.0.0.1:5455" 

		public Client(IConfiguration config)
		{
            var protocol = config["EventServer:Protocol"];
            var addr = config["EventServer:Address"];
            var port = int.Parse(config["EventServer:Port"]);

            zmqReadConnectionString = Global.ZMQConnectionString(protocol, addr, port);
			zmqWriteConnectionString = Global.ZMQConnectionString(protocol, addr, port + 1);
		}

		public LoadEventsReply ReadStream(Guid id)
		{
			var request = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(id));
			var reply = new LoadEventsReply();
			new SendRequestWithRetry(zmqReadConnectionString, request, r =>
									 { 
										 var msg = Encoding.UTF8.GetString(r.ToArray());
										 reply = JsonConvert.DeserializeObject<LoadEventsReply>(msg);
									 });
			return reply;
		}

		public void WriteStream(StoreEventsRequest commit)
		{
			var request = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(commit));
			new SendRequestWithRetry(zmqWriteConnectionString, request, reply =>
				{
					var msg = Encoding.UTF8.GetString(reply.ToArray());
					if (!msg.Equals("OK"))
						throw new Exception("Error - WriteStream in client API - " + msg);
				});
		}
	}
	
    public static class RegisterEventServerServices
    {
        public static IServiceCollection AddEventServerClient(this IServiceCollection services)
        {
            services.AddScoped<IEventServer,Client>();
            return services;
        }
    }

}
