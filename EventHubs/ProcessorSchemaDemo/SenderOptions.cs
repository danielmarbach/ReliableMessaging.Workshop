namespace ProcessorSchemaDemo;

public record SenderOptions
{
    public bool ProduceData { get; set; }

    public int NumberOfDatapointPerChannel { get; set; }

    public string[] Channels { get; set; } = Array.Empty<string>();
}