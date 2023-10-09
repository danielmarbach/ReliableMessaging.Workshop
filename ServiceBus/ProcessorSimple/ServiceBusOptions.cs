namespace Processor;

public class ServiceBusOptions
{
    public required string InputQueue { get; set; }
    public required string DestinationQueue { get; set; }
    public required string TopicName { get; set; }
    public required int NumberOfCommands { get; set; }
}