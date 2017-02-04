using NEvilES.Pipeline;

namespace NEvilES.Tests.Sample
{
    public class SendInviteManager : IProcessCommand<Person.SendInvite>
    {
        private readonly ICommandProcessor _processor;

        public SendInviteManager(ICommandProcessor processor)
        {
            _processor = processor;
        }

        public void Handle(Person.SendInvite command)
        {
            _processor.Process(new Employee.Create()
            {
                StreamId = command.StreamId,
                Person = command.Person
            });

            _processor.Process(new Email.PersonInvited { StreamId = CombGuid.NewGuid(), EmailAddress = command.Email });
        }
    }
}