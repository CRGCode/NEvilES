using System;
using System.Collections.Generic;
using System.Linq;
#pragma warning disable CS8618

namespace Outbox.Abstractions;

public interface IOutboxRepository
{
    void Add(OutboxMessage message);
    IEnumerable<OutboxMessage> GetNext();
    void Remove(int messageId);
}

public interface IOutboxMessage
{
    int Id { get; set; }
    Guid MessageId { get; set; }
    string MessageType { get; set; }
    string Payload { get; set; }
    string Destination { get; set; }
    DateTime CreatedAt { get; set; }
}

public class OutboxMessage : IOutboxMessage
{
    public int Id { get; set; }
    public Guid MessageId { get; set; }
    public string MessageType { get; set; }
    public string Payload { get; set; }
    public string Destination { get; set; }
    public DateTime CreatedAt { get; set; }
}

public interface ISerialize
{
    string ToJson<T>(T obj);
    T FromJson<T>(string json);
}

public class OutboxMessage<T> : OutboxMessage
{
    public OutboxMessage(ISerialize serializer, T message, string destination)
    {
        MessageType = typeof(T).FullName;
        Payload = serializer.ToJson(message);
        Destination = destination;
    }
}

public class InMemoryOutboxRepository : IOutboxRepository
{
    private readonly Dictionary<int, OutboxMessage> data = new();

    private static int _pKey;

    public void Add(OutboxMessage message)
    {
        message.Id = ++_pKey;
        data.Add(message.Id, message);
    }

    public IEnumerable<OutboxMessage> GetNext()
    {
        return data.Select(x => x.Value);
    }

    public void Update(OutboxMessage message)
    {
        data[message.Id] = message;
    }

    public void Remove(int messageId)
    {
        data.Remove(messageId);
    }
}