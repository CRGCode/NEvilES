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

        public class PaidPerson : PayPerson, IEvent { }

        public class PayBonus : Id, ICommand
        {
            public decimal Amount { get; set; }
        }

        public class PaidBonus : PayBonus, IEvent { }

        public class Create : Person.Create, ICommand { }

        public class Aggregate : Person.Aggregate,
            IHandleAggregateCommand<Create,UniqueNameValidation>,
            IHandleAggregateCommand<PayBonus>,
            IHandleAggregateCommand<PayPerson, TaxRuleEngine>
        {
            public void Handle(PayBonus command)
            {
                //RaiseEvent<PaidBonus>(command);
                Raise<PaidBonus>(command);
            }

            public void Handle(PayPerson c, TaxRuleEngine taxCalculator)
            {
                // Use the RuleEngine to do something....
                c.Tax = taxCalculator.Calculate(c.NetAmount);

                RaiseStateless<PaidPerson>(c);
            }

            public void Handle(Create command, UniqueNameValidation validator)
            {
                if (validator.Dispatch(command).IsValid)
                {
                   Handle(command);
                }
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