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

        await administrationClient.CreateQueueAsync(new CreateQueueOptions(serviceBusOptions.Value.InputQueue)
        {
            LockDuration = TimeSpan.FromSeconds(5),
            RequiresSession = true,
        }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}