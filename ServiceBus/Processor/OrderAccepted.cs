namespace Processor;

public record OrderAccepted
{
    public required string OrderId { get; set; }
}