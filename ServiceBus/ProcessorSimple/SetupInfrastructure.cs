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

        var destinationSubscriptionName = $"{serviceBusOptions.Value.DestinationQueue}Subscription";
        if (await administrationClient.SubscriptionExistsAsync(serviceBusOptions.Value.TopicName,
                destinationSubscriptionName, cancellationToken))
        {
            await administrationClient.DeleteSubscriptionAsync(serviceBusOptions.Value.TopicName, destinationSubscriptionName, cancellationToken);
        }

        await administrationClient.CreateSubscriptionAsync(
            new CreateSubscriptionOptions(serviceBusOptions.Value.TopicName, destinationSubscriptionName)
            {
                ForwardTo = serviceBusOptions.Value.DestinationQueue
            }, cancellationToken);

        await administrationClient.DeleteRuleAsync(serviceBusOptions.Value.TopicName, destinationSubscriptionName,
            "$Default", cancellationToken);

        await administrationClient.CreateRuleAsync(serviceBusOptions.Value.TopicName, destinationSubscriptionName,
            new CreateRuleOptions
            {
                Name = "SensorActivated",
                Filter = new CorrelationRuleFilter
                {
                    ApplicationProperties =
                    {
                        { "MessageType", typeof(SensorActivated).FullName }
                    }
                }
            }, cancellationToken);

        var inputQueueSubscriptionName = $"{serviceBusOptions.Value.InputQueue}Subscription";
        if (await administrationClient.SubscriptionExistsAsync(serviceBusOptions.Value.TopicName,
                inputQueueSubscriptionName, cancellationToken))
        {
            await administrationClient.DeleteSubscriptionAsync(serviceBusOptions.Value.TopicName, inputQueueSubscriptionName, cancellationToken);
        }

        await administrationClient.CreateSubscriptionAsync(
            new CreateSubscriptionOptions(serviceBusOptions.Value.TopicName, inputQueueSubscriptionName)
            {
                ForwardTo = serviceBusOptions.Value.InputQueue
            }, cancellationToken);

        await administrationClient.DeleteRuleAsync(serviceBusOptions.Value.TopicName, inputQueueSubscriptionName,
            "$Default", cancellationToken);

        await administrationClient.CreateRuleAsync(serviceBusOptions.Value.TopicName, inputQueueSubscriptionName,
            new CreateRuleOptions
            {
                Name = "AllEventsPublishedUnderNamespace",
                Action = new SqlRuleAction("SET sys.Label = 'Own'"),
                Filter = new SqlRuleFilter("user.MessageType LIKE 'Processor.%'")
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