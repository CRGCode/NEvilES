using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Tests.CommonDomain.Sample
{
    public class RefundManager : IHandleCommand<Customer.Refund>
    {
        private readonly ICommandProcessor processor;

        public RefundManager(ICommandProcessor processor)
        {
            this.processor = processor;
        }

        public void Handle(Customer.Refund command)
        {
            processor.Process(command);
        }
    }
}