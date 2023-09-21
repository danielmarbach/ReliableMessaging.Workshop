using System.Text.Json.Serialization;

namespace SendReceive;

[JsonSerializable(typeof(Message))]
[JsonSerializable(typeof(Envelop<Message>))]
internal partial class StorageJsonContext : JsonSerializerContext
{
}