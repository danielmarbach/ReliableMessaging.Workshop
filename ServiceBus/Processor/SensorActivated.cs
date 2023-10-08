namespace Processor;

public record SensorActivated
{
    public required string ChannelId { get; set; }
}