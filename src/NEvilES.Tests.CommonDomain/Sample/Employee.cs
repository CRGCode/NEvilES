using System;
using NEvilES.Abstractions;

namespace NEvilES.Tests.CommonDomain.Sample
{
    public class Employee
    {
        public class PayPerson : PaidPerson, ICommand { }

        public class PaidPerson : IEvent
        {
            public Guid StreamId { get; set; }
            public decimal NetAmount { get; set; }
            public decimal Tax { get; set; }
        }

        public class PayBonus : PaidBonus, ICommand { }

        public class PaidBonus : IEvent
        {
            public Guid StreamId { get; set; }
            public decimal Amount { get; set; }
        }

        public class Create : Person.Create, ICommand { }

        public class Aggregate : Person.Aggregate,
            IHandleAggregateCommand<Create>,
            IHandleAggregateCommand<PayBonus>,
            IHandleAggregateCommand<PayPerson, TaxRuleEngine>
        {
            public ICommandResponse Handle(PayBonus command)
            {
                //RaiseEvent<PaidBonus>(command);
                Raise<PaidBonus>(command);
                return new CommandCompleted(command.StreamId,nameof(PayBonus));
            }

            public ICommandResponse Handle(PayPerson c, TaxRuleEngine taxCalculator)
            {
                // Use the RuleEngine to do something....
                c.Tax = taxCalculator.Calculate(c.NetAmount);

                RaiseStateless<PaidPerson>(c);
                return new CommandCompleted(c.StreamId,nameof(PayPerson));
            }

            public ICommandResponse Handle(Create command)
            {
                return base.Handle(command);
            }

            //---------------------------------------------------------------------
            // ReSharper disable UnusedMember.Local
            public decimal Bonus;

            private void Apply(PaidBonus ev)
            {
                Bonus = ev.Amount;
            }
        }
    }
}