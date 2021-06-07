using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Tests.CommonDomain.Sample
{
    public class SendInviteManager : IProcessCommand<Person.SendInvite>
    {
        private readonly ICommandProcessor processor;

        public SendInviteManager(ICommandProcessor processor)
        {
            this.processor = processor;
        }

        public void Handle(Person.SendInvite command)
        {
            processor.Process(new Employee.Create
            {
                StreamId = command.StreamId,
                Person = command.Person
            });

            processor.Process(new Email.PersonInvited { StreamId = CombGuid.NewGuid(), EmailAddress = command.Email });
        }
    }
}