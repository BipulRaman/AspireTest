using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AspireApp1.CorrelationId.AzureFunctions;

/// <summary>
/// Base class for Azure Functions with automatic correlation ID handling for different trigger types
/// </summary>
public abstract class CorrelatedFunction
{
    protected readonly ILogger Logger;
    protected readonly IEnhancedCorrelationIdService CorrelationIdService;

    protected CorrelatedFunction(ILogger logger, IEnhancedCorrelationIdService correlationIdService)
    {
        Logger = new CorrelationIdLogger(logger, correlationIdService);
        CorrelationIdService = correlationIdService;
    }

    /// <summary>
    /// Executes a function with correlation ID context and automatic error handling
    /// </summary>
    protected async Task ExecuteWithCorrelationAsync(Func<Task> action, string? functionName = null)
    {
        try
        {
            var context = CorrelationIdService.GetCorrelationContext();
            var name = functionName ?? GetCallingMethodName();
            
            Logger.LogInformation("Function '{FunctionName}' started - Trigger: {TriggerType}", name, context.TriggerType);
            await action();
            Logger.LogInformation("Function '{FunctionName}' completed successfully", name);
        }
        catch (Exception ex)
        {
            var name = functionName ?? GetCallingMethodName();
            Logger.LogError(ex, "Function '{FunctionName}' failed", name);
            throw;
        }
    }

    /// <summary>
    /// Executes a function with correlation ID context and returns result
    /// </summary>
    protected async Task<T> ExecuteWithCorrelationAsync<T>(Func<Task<T>> action, string? functionName = null)
    {
        try
        {
            var context = CorrelationIdService.GetCorrelationContext();
            var name = functionName ?? GetCallingMethodName();
            
            Logger.LogInformation("Function '{FunctionName}' started - Trigger: {TriggerType}", name, context.TriggerType);
            var result = await action();
            Logger.LogInformation("Function '{FunctionName}' completed successfully", name);
            return result;
        }
        catch (Exception ex)
        {
            var name = functionName ?? GetCallingMethodName();
            Logger.LogError(ex, "Function '{FunctionName}' failed", name);
            throw;
        }
    }

    private string GetCallingMethodName()
    {
        var stackTrace = new System.Diagnostics.StackTrace();
        var frame = stackTrace.GetFrame(3); // Skip current method and ExecuteWithCorrelationAsync
        return frame?.GetMethod()?.Name ?? "Unknown";
    }
}

/// <summary>
/// Base class specifically for HTTP-triggered Azure Functions
/// </summary>
public abstract class CorrelatedHttpFunction : CorrelatedFunction
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    protected CorrelatedHttpFunction(ILogger logger, IEnhancedCorrelationIdService correlationIdService) 
        : base(logger, correlationIdService)
    {
    }

    /// <summary>
    /// Initializes correlation ID from HTTP request and executes function
    /// </summary>
    protected async Task<Microsoft.Azure.Functions.Worker.Http.HttpResponseData> ExecuteWithCorrelationAsync(
        Microsoft.Azure.Functions.Worker.Http.HttpRequestData request, 
        Func<Task<Microsoft.Azure.Functions.Worker.Http.HttpResponseData>> action)
    {
        CorrelationIdService.InitializeForHttpTrigger(request);
        
        return await ExecuteWithCorrelationAsync(async () =>
        {
            var response = await action();
            AddCorrelationIdToResponse(response);
            return response;
        });
    }

    /// <summary>
    /// Creates an HTTP response with correlation ID header automatically added
    /// </summary>
    protected Microsoft.Azure.Functions.Worker.Http.HttpResponseData CreateResponse(
        Microsoft.Azure.Functions.Worker.Http.HttpRequestData request, 
        System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
    {
        var response = request.CreateResponse();
        response.StatusCode = statusCode;
        AddCorrelationIdToResponse(response);
        return response;
    }

    /// <summary>
    /// Creates a JSON HTTP response with correlation ID header automatically added
    /// </summary>
    protected async Task<Microsoft.Azure.Functions.Worker.Http.HttpResponseData> CreateJsonResponseAsync<T>(
        Microsoft.Azure.Functions.Worker.Http.HttpRequestData request, 
        T data, 
        System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
    {
        var response = CreateResponse(request, statusCode);
        response.Headers.Add("Content-Type", "application/json");
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        using var writer = new System.IO.StreamWriter(response.Body);
        await writer.WriteAsync(json);
        return response;
    }

    private void AddCorrelationIdToResponse(Microsoft.Azure.Functions.Worker.Http.HttpResponseData response)
    {
        if (!response.Headers.Any(h => h.Key.Equals(CorrelationIdHeader, StringComparison.OrdinalIgnoreCase)))
        {
            response.Headers.Add(CorrelationIdHeader, CorrelationIdService.CorrelationId);
        }
    }
}

/// <summary>
/// Base class for Queue-triggered Azure Functions
/// </summary>
public abstract class CorrelatedQueueFunction : CorrelatedFunction
{
    protected CorrelatedQueueFunction(ILogger logger, IEnhancedCorrelationIdService correlationIdService) 
        : base(logger, correlationIdService)
    {
    }

    /// <summary>
    /// Executes queue function with correlation ID context
    /// </summary>
    protected async Task ExecuteQueueFunctionAsync(string queueMessage, FunctionContext context, Func<Task> action)
    {
        CorrelationIdService.InitializeForQueueTrigger(queueMessage, context);
        await ExecuteWithCorrelationAsync(action, context.FunctionDefinition.Name);
    }

    /// <summary>
    /// Executes queue function with correlation ID context and returns result
    /// </summary>
    protected async Task<T> ExecuteQueueFunctionAsync<T>(string queueMessage, FunctionContext context, Func<Task<T>> action)
    {
        CorrelationIdService.InitializeForQueueTrigger(queueMessage, context);
        return await ExecuteWithCorrelationAsync(action, context.FunctionDefinition.Name);
    }
}

/// <summary>
/// Base class for Service Bus-triggered Azure Functions
/// </summary>
public abstract class CorrelatedServiceBusFunction : CorrelatedFunction
{
    protected CorrelatedServiceBusFunction(ILogger logger, IEnhancedCorrelationIdService correlationIdService) 
        : base(logger, correlationIdService)
    {
    }

    /// <summary>
    /// Executes Service Bus function with correlation ID context
    /// </summary>
    protected async Task ExecuteServiceBusFunctionAsync(object serviceBusMessage, FunctionContext context, Func<Task> action)
    {
        CorrelationIdService.InitializeForServiceBusTrigger(serviceBusMessage, context);
        await ExecuteWithCorrelationAsync(action, context.FunctionDefinition.Name);
    }

    /// <summary>
    /// Executes Service Bus function with correlation ID context and returns result
    /// </summary>
    protected async Task<T> ExecuteServiceBusFunctionAsync<T>(object serviceBusMessage, FunctionContext context, Func<Task<T>> action)
    {
        CorrelationIdService.InitializeForServiceBusTrigger(serviceBusMessage, context);
        return await ExecuteWithCorrelationAsync(action, context.FunctionDefinition.Name);
    }
}

/// <summary>
/// Base class for Timer-triggered Azure Functions
/// </summary>
public abstract class CorrelatedTimerFunction : CorrelatedFunction
{
    protected CorrelatedTimerFunction(ILogger logger, IEnhancedCorrelationIdService correlationIdService) 
        : base(logger, correlationIdService)
    {
    }

    /// <summary>
    /// Executes timer function with correlation ID context
    /// </summary>
    protected async Task ExecuteTimerFunctionAsync(object timerInfo, FunctionContext context, Func<Task> action)
    {
        CorrelationIdService.InitializeForTimerTrigger(timerInfo, context);
        await ExecuteWithCorrelationAsync(action, context.FunctionDefinition.Name);
    }

    /// <summary>
    /// Executes timer function with correlation ID context and returns result
    /// </summary>
    protected async Task<T> ExecuteTimerFunctionAsync<T>(object timerInfo, FunctionContext context, Func<Task<T>> action)
    {
        CorrelationIdService.InitializeForTimerTrigger(timerInfo, context);
        return await ExecuteWithCorrelationAsync(action, context.FunctionDefinition.Name);
    }
}

/// <summary>
/// Base class for Event Hub-triggered Azure Functions
/// </summary>
public abstract class CorrelatedEventHubFunction : CorrelatedFunction
{
    protected CorrelatedEventHubFunction(ILogger logger, IEnhancedCorrelationIdService correlationIdService) 
        : base(logger, correlationIdService)
    {
    }

    /// <summary>
    /// Executes Event Hub function with correlation ID context
    /// </summary>
    protected async Task ExecuteEventHubFunctionAsync(object eventData, FunctionContext context, Func<Task> action)
    {
        CorrelationIdService.InitializeForEventHubTrigger(eventData, context);
        await ExecuteWithCorrelationAsync(action, context.FunctionDefinition.Name);
    }

    /// <summary>
    /// Executes Event Hub function with correlation ID context and returns result
    /// </summary>
    protected async Task<T> ExecuteEventHubFunctionAsync<T>(object eventData, FunctionContext context, Func<Task<T>> action)
    {
        CorrelationIdService.InitializeForEventHubTrigger(eventData, context);
        return await ExecuteWithCorrelationAsync(action, context.FunctionDefinition.Name);
    }
}

/// <summary>
/// Base class for Blob-triggered Azure Functions
/// </summary>
public abstract class CorrelatedBlobFunction : CorrelatedFunction
{
    protected CorrelatedBlobFunction(ILogger logger, IEnhancedCorrelationIdService correlationIdService) 
        : base(logger, correlationIdService)
    {
    }

    /// <summary>
    /// Executes blob function with correlation ID context
    /// </summary>
    protected async Task ExecuteBlobFunctionAsync(object blobTriggerData, FunctionContext context, Func<Task> action)
    {
        CorrelationIdService.InitializeForBlobTrigger(blobTriggerData, context);
        await ExecuteWithCorrelationAsync(action, context.FunctionDefinition.Name);
    }

    /// <summary>
    /// Executes blob function with correlation ID context and returns result
    /// </summary>
    protected async Task<T> ExecuteBlobFunctionAsync<T>(object blobTriggerData, FunctionContext context, Func<Task<T>> action)
    {
        CorrelationIdService.InitializeForBlobTrigger(blobTriggerData, context);
        return await ExecuteWithCorrelationAsync(action, context.FunctionDefinition.Name);
    }
}
