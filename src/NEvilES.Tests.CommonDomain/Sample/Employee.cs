using System;
using NEvilES.Abstractions;

namespace NEvilES.Tests.CommonDomain.Sample
{
    public class Employee
    {
        public abstract class Id : IMessage
        {
            public Guid GetStreamId() => EmployeeId;
            public Guid EmployeeId { get; set; }
        }

        public class PayPerson : Id, ICommand
        {
            public decimal NetAmount { get; set; }
            public decimal Tax { get; set; }
        }

        public class DoNothing : Id, ICommand
        {
        }

        public class PaidPerson : PayPerson, IEvent
        {
        }

        public class PayBonus : Id, ICommand
        {
            public decimal Amount { get; set; }
        }

        public class BonusPaid : Id, IEvent
        {
            public decimal Amount { get; set; }
        }

        public class Create : Person.Create { }

        public class Aggregate : Person.Aggregate,
            IHandleAggregateCommand<Create, UniqueNameValidation>,
            IHandleAggregateCommand<PayPerson, TaxRuleEngine>,
            IHandleAggregateCommand<DoNothing, SomethingMissing>,
            IHandleAggregateCommand<PayBonus>,
            IHandleStatelessEvent<BonusPaid>
        {
            public void Handle(PayPerson c, TaxRuleEngine taxCalculator)
            {
                // Use the RuleEngine to do something....
                c.Tax = taxCalculator.Calculate(c.NetAmount);

                RaiseStateless<PaidPerson>(c);
            }
            public void Handle(DoNothing command, SomethingMissing missing)
            {
                // should never get here as the dependency "SomethingMissing" is never register in DI
                throw new NotImplementedException();
            }

            public void Handle(Create command, UniqueNameValidation validator)
            {
                if (validator.Dispatch(command).IsValid)
                {
                    Handle(command);
                }
            }

            public void Handle(PayBonus c)
            {
                Raise<BonusPaid>(c);
            }

            //---------------------------------------------------------------------
            // ReSharper disable UnusedMember.Local
            public decimal Bonus;

            private void Apply(BonusPaid ev)
            {
                Bonus = ev.Amount;
            }

        }
    }

    public class SomethingMissing
    {
    }
}