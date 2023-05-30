using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Outbox.Abstractions;

public interface IOutboxWorker
{
    void Trigger();
    void MainLoop();
    Task Send();
}

public class OutboxWorkerWorkerThread : IOutboxWorker
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
        var wait = 0;
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Outbox waiting {0}", wait++);

                signal.Wait(cancellationToken);

                Send().Wait(cancellationToken);

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
    }

    public Task StopAsync(CancellationToken ct)
    {
        logger.LogInformation("Outbox service stopping");
        thread.Join();
        return Task.CompletedTask;
    }
}

