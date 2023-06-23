using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Outbox.Abstractions;

public interface ITriggerOutbox
{
    void Trigger();
}

public class OutboxWorkerSendingMessages : ITriggerOutbox, IHostedService
{
    private readonly ILogger<OutboxWorkerSendingMessages> logger;
    private readonly IServiceScopeFactory scopedFactory;
    private readonly ManualResetEventSlim signal;
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly Thread thread;
    private CancellationToken cancellationToken;

    public OutboxWorkerSendingMessages(ILogger<OutboxWorkerSendingMessages> logger, IServiceScopeFactory serviceProvider)
    {
        this.logger = logger;
        scopedFactory = serviceProvider;
        signal = new ManualResetEventSlim(false);

        cancellationTokenSource = new CancellationTokenSource();
        thread = new Thread(MainLoop);
    }

    public void Trigger()
    {
        signal.Set();
        logger.LogInformation("Triggered");
    }

    public async Task Send(IEnumerable<IOutboxMessage> outboxMessages, IServiceBus serviceBus)
    {
        await serviceBus.SendAsync(outboxMessages);
    }

public Task StartAsync(CancellationToken ct)
    {
        logger.LogInformation("Outbox service starting");
        thread.Start();
        cancellationToken = ct;
        return Task.CompletedTask;
    }

    public void MainLoop()
    {
        logger.LogInformation("Entering Worker Main Loop");

        var wait = 0;
        //while (!cancellationTokenSource.Token.IsCancellationRequested)
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Outbox waiting {0}", wait++);

                try
                {
                    signal.Wait(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Outbox service Cancelled whilst waiting for trigger (signal)");
                    break;
                }

                try
                {
                    {
                        using var scope = scopedFactory.CreateScope();
                        var sp = scope.ServiceProvider;

                        var trn = sp.GetRequiredService<IDbTransaction>();
                        var repo = sp.GetRequiredService<IOutboxRepository>();
                        var serviceBus = sp.GetRequiredService<IServiceBus>();
                        var outboxMessages = repo.GetNext().ToArray();
                        // ReSharper disable once MethodSupportsCancellation
                        Send(outboxMessages, serviceBus).Wait(); // We don't want this to ever be cancelled
                        trn.Commit();
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Outbox service Cancelled whilst waiting for ServiceBus to Send()");
                    break;
                }

                signal.Reset();
            }
            catch (Exception e)
            {
                logger.LogError(e, "OutboxWorker Exception");
            }
        }
        logger.LogInformation("Exiting Worker Main Loop");
    }

    public Task StopAsync(CancellationToken ct)
    {
        logger.LogInformation("Outbox service stopping");
        thread.Join();
        logger.LogInformation("Outbox service stopped");
        return Task.CompletedTask;
    }
}
