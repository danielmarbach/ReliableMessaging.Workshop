namespace PullDelivery;

public record EventGridOptions
{
    public required string Endpoint { get; set; }
    public required string AccessKey { get; set; }
    public required string TopicName { get; set; }
    public required string SubscriptionName { get; set; }
}