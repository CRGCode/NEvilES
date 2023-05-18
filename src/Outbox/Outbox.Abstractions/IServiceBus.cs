using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outbox.Abstractions;

public interface IServiceBus
{
    Task SendAsync(IEnumerable<IOutboxMessage> messages);
}