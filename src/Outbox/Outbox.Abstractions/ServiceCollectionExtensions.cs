using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Outbox.Abstractions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOutboxWorker(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<OutboxWorkerSendingMessages>();
            services.AddSingleton<ITriggerOutbox>(s => s.GetRequiredService<OutboxWorkerSendingMessages>());
            services.AddScoped<IServiceBus, OutboxServiceBus>();
            // Use below for local testing... as we want to replace the dependency on AzureServiceBus
            // services.Replace(ServiceDescriptor.Scoped<IServiceBus, LocalServiceBus>());

            if (!string.IsNullOrWhiteSpace(configuration["OutboxSettings:ServiceBusConnection"]))
            {
                services.AddHostedService(x => x.GetRequiredService<OutboxWorkerSendingMessages>());
            }

            return services;
        }
    }
}
