namespace ProcessorSchemaDemo;

public record ProcessorOptions
{
    public double TemperatureThreshold { get; set; }
    public int NumberOfDataPointsToObserve { get; set; }
    public bool RestartFromBeginning { get; set; }
}