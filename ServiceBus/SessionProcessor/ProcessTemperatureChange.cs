namespace SessionProcessor;

public record ProcessTemperatureChange
{
    public DateTimeOffset Published { get; init; }
    public double Current { get; init; }
}