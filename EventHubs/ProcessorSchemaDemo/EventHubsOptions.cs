namespace ProcessorSchemaDemo;

public record EventHubsOptions
{
    public string Name { get; set; }
    public string FullyQualifiedNamespace { get; set; }
}