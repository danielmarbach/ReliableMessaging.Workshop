using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Options;

namespace Processor;

public class SetupInfrastructure : IHostedService
{
    private readonly ServiceBusAdministrationClient administrationClient;
    private readonly IOptions<ServiceBusOptions> serviceBusOptions;

    public SetupInfrastructure(ServiceBusAdministrationClient administrationClient, IOptions<ServiceBusOptions> serviceBusOptions)
    {
        this.administrationClient = administrationClient;
        this.serviceBusOptions = serviceBusOptions;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (await administrationClient.QueueExistsAsync(serviceBusOptions.Value.InputQueue, cancellationToken))
        {
            await administrationClient.DeleteQueueAsync(serviceBusOptions.Value.InputQueue, cancellationToken);
        }

        await administrationClient.CreateQueueAsync(new CreateQueueOptions(serviceBusOptions.Value.InputQueue)
        {
            LockDuration = TimeSpan.FromSeconds(5),
            RequiresDuplicateDetection = true,
            DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(1)
        }, cancellationToken);
        
        if (await administrationClient.QueueExistsAsync(serviceBusOptions.Value.DestinationQueue, cancellationToken))
        {
            await administrationClient.DeleteQueueAsync(serviceBusOptions.Value.DestinationQueue, cancellationToken);
        }
        
        await administrationClient.CreateQueueAsync(new CreateQueueOptions(serviceBusOptions.Value.DestinationQueue), cancellationToken);
        
        if (await administrationClient.TopicExistsAsync(serviceBusOptions.Value.TopicName, cancellationToken))
        {
            await administrationClient.DeleteQueueAsync(serviceBusOptions.Value.TopicName, cancellationToken);
        }

        await administrationClient.CreateTopicAsync(new CreateTopicOptions(serviceBusOptions.Value.TopicName),
            cancellationToken);

        var subscriptionName = $"{serviceBusOptions.Value.DestinationQueue}Subscription";
        if (await administrationClient.SubscriptionExistsAsync(serviceBusOptions.Value.TopicName,
                subscriptionName, cancellationToken))
        {
            await administrationClient.DeleteSubscriptionAsync(serviceBusOptions.Value.TopicName, subscriptionName, cancellationToken);
        }

        await administrationClient.CreateSubscriptionAsync(
            new CreateSubscriptionOptions(serviceBusOptions.Value.TopicName, subscriptionName)
            {
                ForwardTo = serviceBusOptions.Value.DestinationQueue
            }, cancellationToken);

        await administrationClient.CreateRuleAsync(serviceBusOptions.Value.TopicName, subscriptionName,
            new CreateRuleOptions
            {
                Name = "OrderAccepted",
                Filter = new CorrelationRuleFilter
                {
                    ApplicationProperties =
                    {
                        { "MessageType", typeof(OrderAccepted).FullName }
                    }
                }
            }, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (await administrationClient.QueueExistsAsync(serviceBusOptions.Value.InputQueue, cancellationToken))
        {
            await administrationClient.DeleteQueueAsync(serviceBusOptions.Value.InputQueue, cancellationToken);
        }
    }
}