using Azure.Core;
using Azure.Core.Extensions;
using Azure.Data.SchemaRegistry;

namespace ProcessorSchemaDemo;

public static class SchemaRegistryClientBuilderExtensions
{
    public static IAzureClientBuilder<SchemaRegistryClient, SchemaRegistryClientOptions> AddSchemaRegistryClient<TBuilder>(
        this TBuilder builder,
        string fullQualifiedNamespace,
        TokenCredential tokenCredential)
        where TBuilder : IAzureClientFactoryBuilder
    {
        return builder.RegisterClientFactory<SchemaRegistryClient, SchemaRegistryClientOptions>(options => new SchemaRegistryClient(fullQualifiedNamespace, tokenCredential, options));
    }
}