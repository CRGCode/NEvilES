using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Server.Abstractions;

namespace NEvilES.Server;

public class SQLEventStore : IEventStore
{
    public IEnumerable<EventMessage> LoadEvents(object id)
    {
        throw new NotImplementedException();
    }

    public int StoreEvents(object streamId, IEnumerable<Tuple<string, object>> events, int currentVersion, DateTimeOffset timestamp)
    {
        throw new NotImplementedException();
    }
}

public static class RegisterSQLEventStoreServices
{
    public static IServiceCollection AddEventServer(this IServiceCollection services)
    {
        services.AddScoped<IEventStore,SQLEventStore>();
        return services;
    }
}
