using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Options;

namespace Processor;

public class SetupInfrastructure(
    ServiceBusAdministrationClient administrationClient,
    IOptions<ServiceBusOptions> serviceBusOptions)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (await administrationClient.QueueExistsAsync(serviceBusOptions.Value.InputQueue, cancellationToken))
        {
            await administrationClient.DeleteQueueAsync(serviceBusOptions.Value.InputQueue, cancellationToken);
        }

        await administrationClient.CreateQueueAsync(new CreateQueueOptions(serviceBusOptions.Value.InputQueue)
        {
            LockDuration = TimeSpan.FromSeconds(5),
            // TODO
            // 14. set the deduplication detection windows to one minute and enable deduplication
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

        // TODO
        // 15. Create a rule that uses a correlation filter to subscribe to all message types of type SensorActivated

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

        // TODO
        // 16. Create a rule that uses a sql filter and a rule action The SQL filter should subscribe to all message types
        // that are in the namespace "Processor" and override the label on matches with something

    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (await administrationClient.QueueExistsAsync(serviceBusOptions.Value.InputQueue, cancellationToken))
        {
            await administrationClient.DeleteQueueAsync(serviceBusOptions.Value.InputQueue, cancellationToken);
        }
    }
}