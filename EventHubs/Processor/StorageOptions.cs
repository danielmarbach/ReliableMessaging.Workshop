using Azure.Storage.Blobs;

namespace Processor;

public record StorageOptions
{
    public required string ContainerName { get; set; }
    public required string ConnectionString { get; set; }
}