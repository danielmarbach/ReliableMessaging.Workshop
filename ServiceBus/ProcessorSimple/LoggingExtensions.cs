namespace Processor;

static partial class LoggingExtensions
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "Sending #{NumberOfCommands} commands with #{NumberOfDuplicates} duplicates")]
    public static partial void SendWithDuplicates(
        this ILogger logger, int numberOfCommands, int numberOfDuplicates);

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "ActivateSensor command for ID {ChannelId} received.")]
    public static partial void ActivateSensorReceived(
        this ILogger logger, string channelId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Lost the lock while processing sensor activation with ID {ChannelId}.")]
    public static partial void ActivateSensorLockLost(
        this ILogger logger, Exception exception, string channelId);

    [LoggerMessage(
        EventId = 3,
        Message = "#{NumberOfSensors} have been activated.")]
    public static partial void OrderAccepted(
        this ILogger logger, LogLevel logLevel, long numberOfSensors);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "SensorActivated received with label {Subject}.")]
    public static partial void SensorActivatedWithSubject(
        this ILogger logger, string subject);
}