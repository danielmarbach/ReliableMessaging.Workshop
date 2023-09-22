public record TemperatureChanged
{
    public DateTimeOffset Published { get; init; }
    public double Current { get; init; }
};