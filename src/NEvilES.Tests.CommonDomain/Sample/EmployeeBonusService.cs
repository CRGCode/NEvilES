using System.Threading;
using System.Threading.Tasks;
using NEvilES.Abstractions;

namespace NEvilES.Tests.CommonDomain.Sample
{
    public class EmployeeBonusService : IProcessCommandAsync<Employee.PayBonus>
    {
        public async Task HandleAsync(Employee.PayBonus command)
        {
            await Task.Run(() => Thread.Sleep(200));

            //await processor.ProcessAsync(new Employee.BonusPaid(){EmployeeId = command.EmployeeId, Amount = 55M});
        }
    }
}