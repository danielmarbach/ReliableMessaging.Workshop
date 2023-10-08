namespace Processor;

public record ActivateSensor
{
    public required string ChannelId { get; set; }
}