using Microsoft.Azure.Functions.Worker;

namespace AspireApp1.CorrelationId.AzureFunctions;

/// <summary>
/// Enhanced correlation ID service with support for different Azure Functions trigger types
/// </summary>
public interface IEnhancedCorrelationIdService : ICorrelationIdService
{
    /// <summary>
    /// Initializes correlation ID for HTTP triggers
    /// </summary>
    string InitializeForHttpTrigger(Microsoft.Azure.Functions.Worker.Http.HttpRequestData request);

    /// <summary>
    /// Initializes correlation ID for Queue triggers
    /// </summary>
    string InitializeForQueueTrigger(string queueMessage, FunctionContext context);

    /// <summary>
    /// Initializes correlation ID for Service Bus triggers
    /// </summary>
    string InitializeForServiceBusTrigger(object serviceBusMessage, FunctionContext context);

    /// <summary>
    /// Initializes correlation ID for Event Hub triggers
    /// </summary>
    string InitializeForEventHubTrigger(object eventData, FunctionContext context);

    /// <summary>
    /// Initializes correlation ID for Timer triggers
    /// </summary>
    string InitializeForTimerTrigger(object timerInfo, FunctionContext context);

    /// <summary>
    /// Initializes correlation ID for Blob triggers
    /// </summary>
    string InitializeForBlobTrigger(object blobTriggerData, FunctionContext context);

    /// <summary>
    /// Gets correlation context information
    /// </summary>
    CorrelationContext GetCorrelationContext();
}
