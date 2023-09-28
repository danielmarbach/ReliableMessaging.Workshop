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
        Message = "SubmitOrder command for ID {OrderId} received.")]
    public static partial void SubmitOrderReceived(
        this ILogger logger, string orderId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Lost the lock while processing order with ID {OrderId}.")]
    public static partial void SubmitOrderLockLost(
        this ILogger logger, Exception exception, string orderId);

    [LoggerMessage(
        EventId = 3,
        Message = "#{NumberOfOrders} have been accepted.")]
    public static partial void OrderAccepted(
        this ILogger logger, LogLevel logLevel, long numberOfOrders);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "OrderAccepted received with label {Subject}.")]
    public static partial void OrderAcceptedWithSubject(
        this ILogger logger, string subject);
}