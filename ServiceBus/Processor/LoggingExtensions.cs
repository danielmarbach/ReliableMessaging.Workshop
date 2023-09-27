namespace Processor;

static partial class LoggingExtensions
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "Sending #{NumberOfCommands} commands with #{NumberOfDuplicates} duplicates")]
    public static partial void SendWithDuplicates(
        this ILogger logger, int numberOfCommands, int numberOfDuplicates);
}