namespace Processor;

public record struct TemperatureChanged
{
    public DateTimeOffset Published { get; init; }
    public double Current { get; init; }
};