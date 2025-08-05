using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AspireApp1.CorrelationId.AzureFunctions;

/// <summary>
/// Attribute to automatically handle correlation ID for Azure Functions
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class CorrelationIdAttribute : Attribute
{
    public bool AutoGenerateIfMissing { get; set; } = true;
    public bool AddToResponse { get; set; } = true;
    public bool LogFunctionStart { get; set; } = true;
    public bool LogFunctionEnd { get; set; } = true;
}

/// <summary>
/// Helper methods for Azure Functions correlation ID handling
/// </summary>
public static class CorrelationIdHelper
{
    /// <summary>
    /// Processes correlation ID for a function with attribute-based configuration
    /// </summary>
    public static string ProcessCorrelationId(
        HttpRequestData request, 
        ICorrelationIdService correlationIdService, 
        ILogger logger,
        string functionName)
    {
        var correlationId = correlationIdService.GetOrCreateFromHeaders(request.Headers);
        
        // Add to Activity for distributed tracing
        Activity.Current?.SetTag("CorrelationId", correlationId);
        Activity.Current?.SetTag("FunctionName", functionName);
        
        // Log with correlation context
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["FunctionName"] = functionName
        });
        
        logger.LogInformation($"[CorrelationId: {correlationId}] Azure Function '{functionName}' started");
        
        return correlationId;
    }

    /// <summary>
    /// Adds correlation ID to response headers
    /// </summary>
    public static void AddCorrelationIdToResponse(HttpResponseData response, string correlationId)
    {
        response.Headers.Add("X-Correlation-Id", correlationId);
    }

    /// <summary>
    /// Executes a function with correlation ID context and automatic error handling
    /// </summary>
    public static async Task<HttpResponseData> ExecuteWithCorrelationAsync(
        HttpRequestData request,
        ICorrelationIdService correlationIdService,
        ILogger logger,
        string functionName,
        Func<Task<HttpResponseData>> action)
    {
        HttpResponseData response;
        
        try
        {
            var correlationId = ProcessCorrelationId(request, correlationIdService, logger, functionName);
            
            response = await action();
            
            // Ensure correlation ID is in response
            if (!response.Headers.Any(h => h.Key.Equals("X-Correlation-Id", StringComparison.OrdinalIgnoreCase)))
            {
                AddCorrelationIdToResponse(response, correlationId);
            }
            
            logger.LogInformation($"[CorrelationId: {correlationId}] Azure Function '{functionName}' completed successfully");
        }
        catch (Exception ex)
        {
            var correlationId = correlationIdService.CorrelationId;
            logger.LogError(ex, $"[CorrelationId: {correlationId}] Azure Function '{functionName}' failed");
            
            response = request.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            AddCorrelationIdToResponse(response, correlationId);
            await response.WriteStringAsync("Internal server error");
        }
        
        return response;
    }

    /// <summary>
    /// Executes a function that returns data with correlation ID context
    /// </summary>
    public static async Task<HttpResponseData> ExecuteWithCorrelationAsync<T>(
        HttpRequestData request,
        ICorrelationIdService correlationIdService,
        ILogger logger,
        string functionName,
        Func<Task<T>> action)
    {
        return await ExecuteWithCorrelationAsync(request, correlationIdService, logger, functionName, async () =>
        {
            var result = await action();
            var response = request.CreateResponse(System.Net.HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(System.Text.Json.JsonSerializer.Serialize(result));
            return response;
        });
    }
}
