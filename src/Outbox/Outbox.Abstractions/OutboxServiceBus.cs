using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Outbox.Abstractions;

public class OutboxServiceBus : IServiceBus
{
    private readonly ILogger logger;
    private readonly OutboxSettings settings;
    private readonly IOutboxRepository repository;

    public OutboxServiceBus(ILogger<OutboxServiceBus> logger, IOptions<OutboxSettings> settings, IOutboxRepository repository)
    {
        this.logger = logger;
        this.settings = settings.Value;
        this.repository = repository;
    }

    public async Task SendAsync(IEnumerable<IOutboxMessage> messages)
    {
        var topicCache = new Dictionary<string, ServiceBusSender>();
        var connectionTime = TimeSpan.Zero;
        var overallTime = new Stopwatch();
        overallTime.Start();
        await using var client = new ServiceBusClient(settings.ServiceBusConnection);
        var cnt = 0;
        foreach (var message in messages)
        {
            var topic = message.Destination;
            if (!topicCache.TryGetValue(topic, out var sender))
            {
                var sw = new Stopwatch();
                sw.Start();
                sender = client.CreateSender(topic);
                connectionTime = connectionTime.Add(sw.Elapsed);
                topicCache.Add(topic, sender);
            };

            try
            {
                var msg = new ServiceBusMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new MessageEnvelope(message.MessageType, message.Payload))))
                {
                    MessageId = message.MessageId.ToString(),
                };
                logger.LogInformation($"Topic {topic} {message.MessageType} {message.Payload}");
                await sender.SendMessageAsync(msg);
                cnt++;

                repository.Remove(message.Id);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"{DateTime.Now} :: Exception: {e.Message}");
            }
        }

        logger.LogInformation($"Azure ServiceBus Total connection time {connectionTime} Overall time {overallTime.ElapsedMilliseconds} for {cnt} messages sent");
    }

    public class OutboxSettings
    {
        public string ServiceBusConnection { get; set; }
    }
}