using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Outbox.Abstractions;

public interface IOutboxWorker
{
    public void Trigger();
}

public class OutboxWorkerWorkerThread : IOutboxWorker
{
    private readonly ILogger<OutboxWorkerWorkerThread> logger;
    private readonly IServiceBus serviceBus;
    private readonly IOutboxRepository repository;
    private readonly ManualResetEventSlim signal;
    private CancellationTokenSource? cancellationTokenSource;
    private Thread? thread;

    public OutboxWorkerWorkerThread(ILogger<OutboxWorkerWorkerThread> logger,
        IServiceBus serviceBus,
        IOutboxRepository repository)
    {
        this.logger = logger;
        this.serviceBus = serviceBus;
        this.repository = repository;
        signal = new ManualResetEventSlim(false);
    }

    public void Trigger()
    {
        signal.Set();
        logger.LogInformation("Triggered");
    }

    private async Task Send()
    {
        await serviceBus.SendAsync(repository.GetNext());
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Outbox service starting");

        cancellationTokenSource = new CancellationTokenSource();
        thread = new Thread(() =>
        {
            var wait = 0;
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    logger.LogInformation("Outbox waiting {0}", wait++);

                    signal.Wait(cancellationTokenSource.Token);

                    Send().Wait(cancellationTokenSource.Token);

                    signal.Reset();
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Outbox service Cancelled");
                }
                catch (Exception e)
                {
                    logger.LogError(e, "OutboxWorker Exception");
                }
            }
        });

        thread.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Outbox service stopping");
        cancellationTokenSource?.Cancel();
        thread?.Join();
        return Task.CompletedTask;
    }
}

