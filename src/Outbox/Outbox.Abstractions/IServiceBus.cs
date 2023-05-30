using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outbox.Abstractions;

public interface IServiceBus
{
    Task SendAsync(IEnumerable<OutboxMessage> messages);
}

public class LocalServiceBus : IServiceBus
{
    private readonly ILogger<LocalServiceBus> logger;
    //private readonly OutboxSettings settings;
    private readonly IOutboxRepository repository;

    public LocalServiceBus(ILogger<LocalServiceBus> logger,
        //IOptions<OutboxSettings> settings, 
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

