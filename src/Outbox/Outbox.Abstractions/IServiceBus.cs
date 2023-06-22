using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outbox.Abstractions;

public interface IServiceBus
{
    Task SendAsync(IEnumerable<OutboxMessage> messages);
}

public interface IProcessEvent<in T> where T : class, IHaveId
{
    Task HandleEventAsync(T evt);
}

public interface IHaveId
{
    string Id { get; }
}

public class MessageEnvelope
{
    public string Type { get; private set; }
    public string Message { get; private set; }

    public MessageEnvelope(string type, string message)
    {
        Type = type;
        Message = message;
    }
}

public class LocalServiceBus : IServiceBus
{
    private readonly ILogger<LocalServiceBus> logger;
    private readonly IOutboxRepository repository;

    public LocalServiceBus(ILogger<LocalServiceBus> logger,
        IOutboxRepository repository)
    {
        this.logger = logger;
        //this.settings = settings.Value;
        this.repository = repository;
    }

    public Task SendAsync(IEnumerable<OutboxMessage> messages)
    {
        var cnt = 0;
        foreach (var message in messages)
        {
            try
            {
                cnt++;
                logger.LogInformation($"Sending {message.Id} {message.MessageType} {message.Payload}");

                repository.Remove(message.Id);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"{DateTime.Now} :: Exception: {e.Message}");
            }
        }
        logger.LogInformation($"Local ServiceBus {cnt} message(s) sent");

        return Task.CompletedTask;
    }
}

