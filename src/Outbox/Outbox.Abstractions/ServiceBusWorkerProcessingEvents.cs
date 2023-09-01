using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Outbox.Abstractions
{
    //public interface IGetEventHandler
    //{
    //    Type GetEventHandler(TypeInfo type);
    //}
    public interface IProcessEvent<in T> where T : class
    {
        Task HandleEventAsync(T evt);
    }

    public class ServiceBusWorkerProcessingEvents<TEvent> : IHostedService, IDisposable
    {
        private readonly ILogger logger;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ServiceBusClient client;
        private readonly ServiceBusProcessor serviceBusProcessor;
        private readonly Dictionary<string, TypeInfo> typeLookup;

        public ServiceBusWorkerProcessingEvents(
            IOptions<ServiceBusOptions> options,
            ILogger<ServiceBusWorkerProcessingEvents<TEvent>> logger,
            IServiceScopeFactory sf)
        {
            this.logger = logger;
            scopeFactory = sf;

            typeLookup = typeof(TEvent).Assembly
                .DefinedTypes.Where(t => t.DeclaringType == null)
                .ToDictionary(k => k.FullName, v => v);
            client = new ServiceBusClient(options.Value.ConnectionString);

            var serviceBusProcessorOptions = new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 1,
                AutoCompleteMessages = false,
            };

            if (string.IsNullOrWhiteSpace(options.Value.TopicSubscription))
                throw new Exception("Must provide ServiceBusOptions.TopicSubscription");

            if (!options.Value.TopicSubscription.Contains(':'))
                throw new Exception("Invalid ServiceBusOptions.TopicSubscription, format 'TopicName:Subscription' ie. the separator ':' is critical");
            var subscription = options.Value.TopicSubscription.Split(":");
            serviceBusProcessor = client.CreateProcessor(subscription[0], subscription[1], serviceBusProcessorOptions);
            serviceBusProcessor.ProcessMessageAsync += ProcessMessagesAsync;
            serviceBusProcessor.ProcessErrorAsync += ProcessErrorAsync;
        }

        private async Task ProcessMessagesAsync(ProcessMessageEventArgs args)
        {
            var scope = scopeFactory.CreateScope();
            try
            {
                var envelope = JsonConvert.DeserializeObject<MessageEnvelope>(args.Message.Body.ToString())!;

                if (typeLookup.TryGetValue(envelope.Type, out var type))
                {
                    dynamic obj = JsonConvert.DeserializeObject(envelope.Message, type)!;

                    //var ht = GetEventHandler(type);
                    
                    var ht = typeof(IProcessEvent<>).MakeGenericType(type);
                    dynamic processor = scope.ServiceProvider.GetRequiredService(ht);

                    logger.LogDebug($"Attempting to ProcessEvent<{envelope.Type}>\n{envelope.Message}");
                    await processor.HandleEventAsync(obj);
                }
                else
                {
                    logger.LogInformation($"envelope.Type not found - '{envelope.Type}'\n{envelope.Message}");
                }

                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Abandon Message");
                await args.AbandonMessageAsync(args.Message);  // this will cause the message to be resent from Azure ServiceBus
            }
            finally
            {
                scope.Dispose();
            }
        }

        //public abstract Type GetEventHandler(TypeInfo type);

        private Task ProcessErrorAsync(ProcessErrorEventArgs arg)
        {
            logger.LogCritical(arg.Exception, "EventProcessor encountered an exception");
            logger.LogDebug($"- ErrorSource: {arg.ErrorSource}");
            logger.LogDebug($"- Entity Path: {arg.EntityPath}");
            logger.LogDebug($"- FullyQualifiedNamespace: {arg.FullyQualifiedNamespace}");

            return Task.CompletedTask;  // TODO this is bad! Should we complete the task
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation($"Starting {GetType().Name}");

            await serviceBusProcessor.StartProcessingAsync(stoppingToken);
        }

        public async Task StopAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation($"Stopping {GetType().Name}");

            await serviceBusProcessor.CloseAsync(stoppingToken);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual async void Dispose(bool disposing)
        {
            if (!disposing) return;

            await serviceBusProcessor.DisposeAsync();
            await client.DisposeAsync();
        }
    }

#pragma warning disable CS8618
    public class ServiceBusOptions
    {
        public string ConnectionString { get; set; }

        public string QueueName { get; set; }

        public string TopicSubscription { get; set; }
    }
}