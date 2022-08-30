using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Configuration;
using CRG.ES.ClientAPI;
using CRG.ES.CommandProcessor;

namespace CRG.ES.SmokeTest
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			//new CounterTest(50).SmokeIt(); return;

			//AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

			var x = new Random(DateTime.Now.Millisecond);
			Thread.Sleep(500);
			var builder = new ContainerBuilder();

			builder.RegisterModule(new ConfigurationSettingsReader("Autofac"));
			//builder.RegisterType<EventStoreStreamReader>().As<IAccessEventStore>();
			builder.RegisterType<HiLoServerGeneratedCounter>().As<ICounterGenerator>().SingleInstance();

			builder.RegisterModule(new ConfigCommandProcessing<SampleAggregate>("Console",
				new[] {typeof (SampleAggregate).Assembly},
			    new[] {typeof (SampleAggregate).Assembly}));

			builder.RegisterInstance(new NullUnitOfWork()).As<IUnitOfWork>();
			builder.RegisterAssemblyTypes(typeof(SampleEventHandler).Assembly).AsClosedTypesOf(typeof(IHandleEvent<>));
			var container = builder.Build();

			var reader = container.Resolve<IAccessEventStore>();

			var server = container.Resolve<ICounterGenerator>();
			var cnt = server.GetNextCounter();

			Console.WriteLine("Persistent Counter {0}", PersistentCounter.ReadCounter());
			PersistentCounter.WriteCounter(5);
			Console.WriteLine("Persistent Counter {0}", PersistentCounter.ReadCounter());

			var cpu = container.Resolve<ICommandProcessingUnit>();
			var repo = container.Resolve<IRepository>();

			var testId = Guid.NewGuid();
			var test = new CreateSampleAggregate
				{
					Id = testId,
					Name = "Testing",
					Email = "test@test.com",
				};
			Console.WriteLine("Running '{0}' command", test.GetType().Name);
			cpu.Process(test);

			try
			{
				cpu.Process(new UploadFile { Id = testId, Filename = "cause error in eventhandler" });
			}
			catch(Exception)
			{
				Console.WriteLine("Event handler error....");
			}

			//RunDupicateTest(test, cpu);

			var aggregate = repo.GetById<SampleAggregate>(testId);
			Console.WriteLine("Successfully retrieved aggregate '{0}' - {1}", aggregate.GetType().Name, aggregate.Id);

			Console.WriteLine("Thrashing");
			const int count = 6;
			var tasks = new Task[count];
			for (var i = 0; i < count; i += 2)
			{
				var msg = i + 1;
				tasks[i] = Task.Factory.StartNew(() =>
				{
					DoWork2(container.BeginLifetimeScope(), Guid.NewGuid());
				});
				tasks[i + 1] = Task.Factory.StartNew(() =>
				{
					DoWork(container.BeginLifetimeScope(), testId, msg);
				});
			}

			Task.WaitAll(tasks);

			Console.WriteLine("\nAttempt to retrieve stream history for '{0}' - {1}", aggregate.GetType().Name, aggregate.Id);
			var history = reader.Get(aggregate.Id.ToString()).ToArray();
			foreach(var eventData in history)
			{
				Console.WriteLine("{0} {1}", eventData.Type, eventData.Version);
			}
			Console.WriteLine("Total number of events '{0}'", history.Count());

			Console.ReadLine();
		}

		private static void RunDupicateTest(CreateSampleAggregate test, ICommandProcessingUnit cpu)
		{
			Console.WriteLine("Running Duplicate Command '{0}' SHOULD ERROR", test.GetType().Name);
			try
			{
				cpu.Process(test);
			}
			catch (Exception)
			{
				Console.WriteLine("OK Error occurred....");
			}
		}

		private static void Errorlogger(string message, Exception ex)
		{
			Console.WriteLine("Exception '{0}'/n{1}", message,ex.Message);
		}

		private static void DoWork(IComponentContext scope, Guid id, int i)
		{
			var changeName = new ChangeName { Id = id, Name = "Name " + i };
			var cpu = scope.Resolve<ICommandProcessingUnit>();
			try
			{
				//Console.WriteLine("Change Name - {0}", i);
				cpu.Process(changeName);
			}
			catch(Exception e)
			{
				Console.WriteLine("ChangeName LOST - {0}\nException - {1}", changeName.Name, e.Message);
			}
		}

		private static void DoWork2(IComponentContext scope, Guid id)
		{
			var cpu = scope.Resolve<ICommandProcessingUnit>();
			cpu.Process(new CreateSampleAggregate { Id = id, Name = "Testing", Email = "test@test.com"});
			for (int i = 0; i < 5; i++)
			{
				var changeName = new ChangeName { Id = id, Name = "Name " + i };
				try
				{
					//Console.WriteLine("Change Name - {0}", i);
					cpu.Process(changeName);
				}
				catch(Exception e)
				{
					Console.WriteLine("DoWork2 ChangeName LOST - {0}\nException - {1}", changeName.Name, e.Message);
				}
			}
			cpu.Process(new AddWorkItem(){Id = id, Description = "All done!"});
		}

		private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
		{
			Console.WriteLine(e.ExceptionObject.ToString());
			Console.WriteLine("Press Enter to continue");
			Console.ReadLine();
		}
	}

	internal class NullUnitOfWork : IUnitOfWork
	{
		public void SaveChanges() {}

		public int GetChangeCount()
		{
			return 0;
		}

		public string[] WhatHasChanged()
		{
			return new string[] { };
		}

		public int MaxNumberOfRequests { get; set; }
	}
}
