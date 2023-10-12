using System.Text.Json.Serialization;

namespace SendReceive;

[JsonSerializable(typeof(ActivateSensor))]
[JsonSerializable(typeof(Envelop<ActivateSensor>))]
internal partial class StorageJsonContext : JsonSerializerContext
{
}