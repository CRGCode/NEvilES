//using System;
//using Autofac;
//using NetMQ;

//namespace CRG.ES.ClientAPI
//{
//	public class EventStoreConfig : Module
//	{
//		private string esServerConnectionString;
//		private int basePort;

//		public int BasePort
//		{
//			get { return basePort; }
//			set 
//			{ 
//				if (basePort == 0)
//					basePort = value;
//				else
//				{
//					throw new ApplicationException("EventStore 'BasePort' property can only be set once, you can't modify it!");
//				}
//			}
//		}

//		public EventStoreConfig(string connectionString)
//		{
//			esServerConnectionString = connectionString;
//		}

//		public EventStoreConfig()
//		{
//		}

//		protected override void Load(ContainerBuilder builder)
//		{
//			if (string.IsNullOrEmpty(esServerConnectionString))
//			{
//				//using BasePort property to configure server connection 
//				if (basePort == 0)
//					throw new ApplicationException("Must set EventStore BasePort property");
//				var ip = Global.GetIPAddress();

//				esServerConnectionString = $"tcp;{ip};{basePort}";
//			}

//			var esParams = esServerConnectionString.Split(';');
//			if(esParams.Length != 3)
//			{
//				throw new ApplicationException("Error - ES server params not defined correctly");
//			}
//			builder.Register(c => new EventStore(esParams[0], esParams[1], int.Parse(esParams[2]))).As<IEventStore>().SingleInstance();
//			builder.Register(c => new StoreCounter(esParams[0], esParams[1], int.Parse(esParams[2]))).As<I_ZMQCounter>().SingleInstance();

//			builder.RegisterType<EventStoreRepository>().As<IRepository>();
//			builder.RegisterType<EventStoreStreamReader>().As<IAccessEventStore>();
//		}
//	}
//}