using System;

namespace Outbox.Abstractions;

#pragma warning disable CS8618
public class OutboxMessage : IOutboxMessage
{
    public int Id { get; set; }
    public Guid MessageId { get; set; }
    public string MessageType { get; set; }
    public string Payload { get; set; }
    public string Destination { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OutboxMessage<T> : OutboxMessage
{
    public OutboxMessage(ISerialize serializer, T message, string destination)
    {
        MessageId = Guid.NewGuid();
        MessageType = typeof(T).FullName;
        Payload = serializer.ToJson(message);
        Destination = destination;
    }
}