namespace SessionProcessor;

public class ServiceBusOptions
{
    public required string InputQueue { get; set; }
    public required string DestinationQueue { get; set; }
    public required string TopicName { get; set; }
    public int NumberOfCommandsPerChannel { get; set; }
    public string[] Channels { get; set; } = Array.Empty<string>();
    public double TemperatureThreshold { get; set; }
    public int NumberOfDataPointsToObserve { get; set; }
}