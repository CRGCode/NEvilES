using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NEvilES.Server.Abstractions;
using NEvilES.Server.ClientApi;

namespace NEvilES.Server;

public class SampleClient : BackgroundService
{
    private readonly ILogger<SampleClient> logger;
    private readonly IEventServer server;

    public SampleClient(ILogger<SampleClient> logger, IEventServer server)
    {
        this.logger = logger;
        this.server = server;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var id = Guid.NewGuid();
            logger.LogInformation("Client ReadStream : {id}", id);
            server.ReadStream(id);

            await Task.Delay(1000,stoppingToken);
            logger.LogInformation("Client WriteStream : {id}", id);
            server.WriteStream(new StoreEventsRequest()
            {
                StreamId = id,
                Events = new List<Tuple<string, object>>(){},
                Username = "Test",
                Timestamp = DateTimeOffset.Now,
            });
            await Task.Delay(1000,stoppingToken);
        }
    }
}