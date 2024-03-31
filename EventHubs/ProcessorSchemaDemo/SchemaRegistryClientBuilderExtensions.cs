using Azure.Core.Extensions;
using Azure.Data.SchemaRegistry;
using Azure.Identity;

namespace ProcessorSchemaDemo;

public static class SchemaRegistryClientBuilderExtensions
{
    public static IAzureClientBuilder<SchemaRegistryClient, SchemaRegistryClientOptions> AddSchemaRegistryClient<TBuilder>(
        this TBuilder builder,
        string fullQualifiedNamespace)
        where TBuilder : IAzureClientFactoryBuilder
    {
        return builder.RegisterClientFactory<SchemaRegistryClient, SchemaRegistryClientOptions>(options => new SchemaRegistryClient(fullQualifiedNamespace, new DefaultAzureCredential(), options));
    }
}