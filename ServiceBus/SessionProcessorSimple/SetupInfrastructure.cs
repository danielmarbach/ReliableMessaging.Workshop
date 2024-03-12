using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Options;

namespace SessionProcessor;

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

        // TODO
        // Create the queue with session support
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (await administrationClient.QueueExistsAsync(serviceBusOptions.Value.InputQueue, cancellationToken))
        {
            await administrationClient.DeleteQueueAsync(serviceBusOptions.Value.InputQueue, cancellationToken);
        }
    }
}