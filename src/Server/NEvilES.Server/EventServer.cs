using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetMQ;
using NetMQ.Sockets;
using NEvilES.Server.Abstractions;
using Newtonsoft.Json;

namespace NEvilES.Server
{
    public class EventServer : BackgroundService
	{
        private readonly ILogger<EventServer> logger;

		private readonly IEventStore es;
		private const string Protocol = "tcp";

		private readonly string address;
		private readonly int basePort;
		private readonly NetMQPoller poller;

		public EventServer(ILogger<EventServer> esLogger, IConfiguration config, IEventStore eventStore)
            //, string connection, string addr, int port, int resend)
        {
            logger = esLogger;
            es = eventStore;
			//var csb = new SqlConnectionStringBuilder(connection);
            var dbConfig = ""; // $"{csb.DataSource} {csb.InitialCatalog}";
            var addr = config["EventServer:Address"];
            var port = int.Parse(config["EventServer:Port"]);
			logger.LogInformation("DB Config {0}",dbConfig);
            logger.LogInformation("Address {0}",addr);
            logger.LogInformation("Base Port {0}",port);

			address = addr;
			basePort = port;
			poller = new NetMQPoller();
			//cts = new CancellationTokenSource();
			//var token = cts.Token;
			//task = resend == 0 ? new Task(DelegatePolling, token) : new Task(x => ResendEvent((int)x), resend, token);
		}

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Poller started");

            return new Task(DelegatePolling, stoppingToken);
        }

		//public void Start()
		//{
		//	task.Start();
		//}

		//public void Stop()
		//{
		//	if(poller.IsRunning)
		//		poller.Stop();
		//	cts.Cancel();
		//}

		private NetMQSocket pub;

		public void DelegatePolling()
		{
            try
            {
                using var load = new ResponseSocket();
                load.Bind(Global.ZMQConnectionString(Protocol, address, basePort));
                using var store = new ResponseSocket();
                using var catchup = new ResponseSocket();
                using var counter = new ResponseSocket();
                using var socket = pub = new PublisherSocket();
                store.Bind(Global.ZMQConnectionString(Protocol, address, basePort + 1));
                pub.Bind(Global.ZMQConnectionString(Protocol, address, basePort + 2));
                catchup.Bind(Global.ZMQConnectionString(Protocol, address, basePort + 3));
                counter.Bind(Global.ZMQConnectionString(Protocol, address, basePort + 4));

                catchup.ReceiveReady += CatchupReqHandler;
                load.ReceiveReady += LoadReqHandler;
                store.ReceiveReady += StoreReqHandler;
                counter.ReceiveReady += CounterReqHandler;

                poller.Add(load);
                poller.Add(store);
                poller.Add(catchup);
                poller.Add(counter);

                poller.Run();
            }
            catch (Exception e)
            {
                logger.LogCritical("Fatal 0MQ Poller Exception", e);
                //cts.Cancel();
                throw;
            }

            logger.LogInformation("Exiting 0MQ Poller");
		}

		private void CatchupReqHandler(object sender, NetMQSocketEventArgs e)
		{
			var msg = e.Socket.ReceiveFrameString();
			var catchUpReq = JsonConvert.DeserializeObject<CatchUpRequest>(msg);
			var pubEvents = es.LoadEvents(catchUpReq);

			e.Socket.SendFrame(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(pubEvents)));
		}

		private static void CounterReqHandler(object sender, NetMQSocketEventArgs e)
		{
			byte[] reply;
			var msg = e.Socket.ReceiveFrameString();
			if(msg.Length > 1)
			{
				// write
				var count = int.Parse(msg);
				PersistentCounter.WriteCounter(count);
				reply = Encoding.UTF8.GetBytes("Ok");
			}
			else
			{
				// read
				reply = Encoding.UTF8.GetBytes(PersistentCounter.ReadCounter().ToString());
			}
			e.Socket.SendFrame(reply);
		}

		private void LoadReqHandler(object sender, NetMQSocketEventArgs e)
		{
			var msg = e.Socket.ReceiveFrameString();
			var streamId = JsonConvert.DeserializeObject<Guid>(msg);
			var events = es.LoadEvents(streamId).ToList();
			var reply = new LoadEventsReply { Events = events };
			e.Socket.SendFrame(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(reply)));
		}

		private void StoreReqHandler(object sender, NetMQSocketEventArgs e)
		{
			var bytes = e.Socket.ReceiveFrameBytes();
			var request = JsonConvert.DeserializeObject<StoreEventsRequest>(Encoding.UTF8.GetString(bytes));
			int counter;
			try
			{
				counter = es.StoreEvents(request.StreamId, request.Events, request.CurrentVersion,
										 request.Timestamp);
			}
			//catch(SqlException sql)
			//{
			//	var error = sql.Errors[0];
			//	if(error.Number == 2627)
			//	{
			//		logger.Error("D");
			//		e.Socket.Send(Encoding.UTF8.GetBytes("DUP"));
			//		return;
			//	}
			//	logger.Fatal("SQL Error number {0}", error.Number);
			//	logger.Info("SQL Stack Trace {0}", sql.StackTrace);
			//	e.Socket.Send(Encoding.UTF8.GetBytes("ERR"));
			//	return;
			//}
			catch(Exception ex)
			{
				logger.LogCritical(ex,"Non SQL Error - {0}",ex.Message);
				e.Socket.SendFrame(Encoding.UTF8.GetBytes("ERR"));
				return;
			}
			e.Socket.SendFrame(Encoding.UTF8.GetBytes("OK"));

            var pubEvents = new PublishEvents(counter)
            {
                Events = request.Events.Select(x => new EventMessage(request.Source, request.StreamId,
                    request.CurrentVersion, x.Item1, Encoding.UTF8.GetBytes(x.Item2.ToString()))).ToList()
            };
			pub.SendFrame(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(pubEvents)));
		}
    }

    public class EventServerOptions
    {
    }

    public static class PersistentCounter
    {
        public static void WriteCounter(long count)
        {
            throw new NotImplementedException();
        }

        public static long ReadCounter()
        {
            throw new NotImplementedException();
        }
    }

    public interface IEventStore 
    {
        IEnumerable<EventMessage> LoadEvents(object id);

        int StoreEvents(object streamId, IEnumerable<Tuple<string, object>> events, int currentVersion, DateTimeOffset timestamp);
    }
}