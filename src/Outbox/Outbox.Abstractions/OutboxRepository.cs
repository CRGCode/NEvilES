using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS8618

namespace Outbox.Abstractions;

public interface IOutboxRepository
{
    void Add(IOutboxMessage message);
    IEnumerable<IOutboxMessage> GetNext();
    void Remove(int messageId);

    void SaveChanges();
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

//public class OutboxMessage<T> : OutboxMessage
//{
//    public OutboxMessage(ISerialize serializer, T message, string destination)
//    {
//        MessageId = Guid.NewGuid();
//        MessageType = typeof(T).FullName;
//        Payload = serializer.ToJson(message);
//        Destination = destination;
//    }
//}

public interface ISerialize
{
    string ToJson<T>(T obj);
    T FromJson<T>(string json);
}

public class InMemoryOutboxRepository : IOutboxRepository
{
    private readonly Dictionary<int, IOutboxMessage> data = new();

    private static int _pKey;

    public void Add(IOutboxMessage message)
    {
        message.Id = ++_pKey;
        data.Add(message.Id, message);
    }

    public IEnumerable<IOutboxMessage> GetNext()
    {
        return data.Select(x => x.Value);
    }

    public void Remove(int messageId)
    {
        data.Remove(messageId);
    }

    public void SaveChanges()
    {
    }
}