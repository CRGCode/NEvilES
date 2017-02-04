using System;

namespace NEvilES.Tests.Sample
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

        public class Create : Person.Create
        {
        }

        public class Aggregate : Person.Aggregate,
            IHandleAggregateCommand<Create>,
            IHandleAggregateCommand<PayBonus>,
            IHandleAggregateCommand<PayPerson, TaxRuleEngine>
        {
            public void Handle(PayBonus command)
            {
                RaiseEvent<PaidBonus>(command);
            }

            public void Handle(PayPerson c, TaxRuleEngine taxCalculator)
            {
                // Use the RuleEngine to do something....
                c.Tax = taxCalculator.Calculate(c.NetAmount);

                RaiseStatelessEvent<PaidPerson>(c);
            }

            public void Handle(Create command)
            {
                base.Handle(command);
            }

            //---------------------------------------------------------------------
            // ReSharper disable UnusedMember.Local
            public decimal bonus;

            private void Apply(PaidBonus ev)
            {
                bonus = ev.Amount;
            }
        }
    }
}