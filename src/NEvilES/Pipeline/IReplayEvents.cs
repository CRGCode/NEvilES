using System;
using System.Threading.Tasks;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Abstractions.Pipeline.Async;
using NEvilES.Pipeline.Async;

namespace NEvilES.Pipeline
{
    public static class ReplayEvents
    {
        public static void Replay(IFactory factory, IAggregateHistory reader,  Int64 from = 0, Int64 to = 0)
        {
            foreach (var commit in reader.Read(from,to))
            {
                ReadModelProjectorHelper.Project(new CommandResult(commit), factory, CommandContext.Null());
            }
        }


        public static async Task ReplayAsync(IFactory factory, IAsyncAggregateHistory reader,  Int64 from = 0, Int64 to = 0)
        {
            foreach (var commit in await reader.ReadAsync(from,to))
            {
                await ReadModelProjectorHelperAsync.ProjectAsync(new CommandResult(commit), factory, CommandContext.Null());
            }
        }
    }

}