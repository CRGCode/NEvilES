using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Outbox.Abstractions;

public interface IOutboxWorker
{
    void Trigger();
}

public class OutboxWorkerWorkerThread : IOutboxWorker, IHostedService
{
    private readonly ILogger<OutboxWorkerWorkerThread> logger;
    private readonly IServiceBus serviceBus;
    private readonly IOutboxRepository repository;
    private readonly ManualResetEventSlim signal;
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly Thread thread;
    private CancellationToken cancellationToken;

    public OutboxWorkerWorkerThread(ILogger<OutboxWorkerWorkerThread> logger,
        IServiceBus serviceBus,
        IOutboxRepository repository)
    {
        this.logger = logger;
        this.serviceBus = serviceBus;
        this.repository = repository;
        signal = new ManualResetEventSlim(false);
        
        cancellationTokenSource = new CancellationTokenSource();
        thread = new Thread(MainLoop);
    }

    public void Trigger()
    {
        signal.Set();
        logger.LogInformation("Triggered");
    }

    public async Task Send()
    {
        var outboxMessages = repository.GetNext().ToArray();
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
                }

                try
                {
                    Send().Wait();//(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Outbox service Cancelled whilst waiting for ServiceBus to Send()");
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

