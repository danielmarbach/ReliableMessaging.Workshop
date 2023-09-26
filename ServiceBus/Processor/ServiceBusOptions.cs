namespace Processor;

public class ServiceBusOptions
{
    public required string InputQueue { get; set; }
    public required string TopicName { get; set; }
}