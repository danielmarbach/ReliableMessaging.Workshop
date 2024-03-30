namespace ProcessorSchemaDemo;

public record StorageOptions
{
    public required string ContainerName { get; set; }
    public required string ConnectionString { get; set; }
}