using System;
using NLog;

namespace CRG.ES.SmokeTest
{
	public class SampleAggregate : AggregateBase
	{
		public SampleAggregate(Guid id, string name, string email)
			: this()
		{
			RaiseEvent(new SampleAggregateCreated
				{
					Id = id,
					Name = name,
					Email = email,
				});
		}

		public void ChangeName(string newName)
		{
			RaiseEvent(new NameChanged { Id = Id, Name=newName });
		}

		public void UploadFile(string filename)
		{
			RaiseEvent(new UploadFile { Id = Id, Filename = filename });
		}

		public void AddItem(string description)
		{
			RaiseEvent(new AddWorkItem() { Id = Id, Description = description });
		}

		//------------------------------------------------------------------------------------------------------------------------

		private SampleAggregate()
		{
		}

		private void Apply(SampleAggregateCreated e)
		{
			Id = e.Id;
		}

		private void Apply(NameChanged e)
		{
		}
		private void Apply(UploadFile e)
		{
		}
		private void Apply(AddWorkItem e)
		{
		}
	}

	public class SampleAggregateHandler :
		IHandleCommand<CreateSampleAggregate>,
		IHandleCommand<UploadFile>,
		IHandleCommand<AddWorkItem>,
	    IHandleCommand<ChangeName>
	{
		private readonly IRepository repository;

		public SampleAggregateHandler(IRepository repository)
		{
			this.repository = repository;
		}

		public CommandResult Handle(CreateSampleAggregate c)
		{
			var aggregate = new SampleAggregate(c.Id, c.Name, c.Email);
			return new CommandResult(repository.Save(aggregate));
		}

		public CommandResult Handle(ChangeName c)
		{
			var aggregate = repository.GetById<SampleAggregate>(c.Id);
			aggregate.ChangeName(c.Name);
			return new CommandResult(repository.Save(aggregate));
		}

		public CommandResult Handle(UploadFile c)
		{
			var aggregate = repository.GetById<SampleAggregate>(c.Id);
			aggregate.UploadFile(c.Filename);
			return new CommandResult(repository.Save(aggregate));
		}

		public CommandResult Handle(AddWorkItem c)
		{
			var aggregate = repository.GetById<SampleAggregate>(c.Id);
			aggregate.AddItem(c.Description);
			return new CommandResult(repository.Save(aggregate));
		}
	}

	public class CreateSampleAggregate : Command
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
	}

	public class ChangeName : Command
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
	}

	public class AddWorkItem : IMessage
	{
		public Guid Id { get; set; }
		public string Description { get; set; }
	}

	public class UploadFile : Command
	{
		public Guid Id { get; set; }
		public string Filename { get; set; }
	}

	public class SampleEventHandler :
		IHandleEvent<SampleAggregateCreated>,
		IHandleEvent<UploadFile>,
		IHandleEvent<NameChanged>,
		IHandleEvent<AddWorkItem>
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		public void Handle(SampleAggregateCreated e)
		{
			logger.Debug("{0} Created", e.Id);
		}

		private static int cnt;
		public void Handle(NameChanged e)
		{
			logger.Debug("{0} NameChanged {1} ({2})", e.Id, e.Name, cnt++);
		}

		public void Handle(UploadFile e)
		{
			// cause error
			var x = int.Parse(e.Filename);
			Console.WriteLine(x);
		}

		public void Handle(AddWorkItem e)
		{
			logger.Debug("{0} AddWorkItem {1}", e.Id, e.Description);
		}
	}


	[Serializable]
	public class SampleAggregateCreated : IBusinessEvent
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
	}

	[Serializable]
	public class NameChanged : IEvent
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
	}


}