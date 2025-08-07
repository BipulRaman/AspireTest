using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

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

/// <summary>
/// Enhanced correlation ID service implementation with multi-trigger support
/// </summary>
public class EnhancedCorrelationIdService : CorrelationIdService, IEnhancedCorrelationIdService
{
    private readonly CorrelationIdOptions _options;
    private static readonly AsyncLocal<CorrelationContext> _correlationContext = new();

    public EnhancedCorrelationIdService(IOptions<CorrelationIdOptions> options)
    {
        _options = options.Value;
    }

    public string InitializeForHttpTrigger(Microsoft.Azure.Functions.Worker.Http.HttpRequestData request)
    {
        if (!_options.Triggers.Http.Enabled)
            return GenerateCorrelationId();

        string? correlationId = null;

        // Try to get from headers first and capture additional headers
        correlationId = GetOrCreateFromHeaders(request.Headers, _options.AdditionalHeaders);

        // Try query parameters if enabled and header not found
        if (string.IsNullOrWhiteSpace(correlationId) && _options.Triggers.Http.UseQueryParameter)
        {
            correlationId = ExtractFromQueryParameters(request.Url);
        }

        // Generate if not found and auto-generation is enabled
        if (string.IsNullOrWhiteSpace(correlationId) && _options.AutoGenerate)
        {
            correlationId = GenerateCorrelationId();
        }

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            SetCorrelationId(correlationId);
            SetCorrelationContext(new CorrelationContext
            {
                CorrelationId = correlationId,
                TriggerType = "Http",
                Timestamp = DateTime.UtcNow,
                Source = "HttpTrigger"
            });
        }

        return correlationId ?? string.Empty;
    }

    public string InitializeForQueueTrigger(string queueMessage, FunctionContext context)
    {
        if (!_options.Triggers.Queue.Enabled)
            return GenerateCorrelationId();

        string? correlationId = null;

        // Try to parse from message body if enabled
        if (_options.Triggers.Queue.ParseFromMessageBody)
        {
            correlationId = ExtractFromJsonMessage(queueMessage, _options.Triggers.Queue.MessageBodyPropertyPath);
        }

        // Generate if not found
        if (string.IsNullOrWhiteSpace(correlationId) && _options.AutoGenerate)
        {
            correlationId = GenerateCorrelationId();
        }

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            SetCorrelationId(correlationId);
            SetCorrelationContext(new CorrelationContext
            {
                CorrelationId = correlationId,
                TriggerType = "Queue",
                Timestamp = DateTime.UtcNow,
                Source = context.FunctionDefinition.Name,
                AdditionalProperties = { ["QueueName"] = ExtractQueueName(context) }
            });
        }

        return correlationId ?? string.Empty;
    }

    public string InitializeForServiceBusTrigger(object serviceBusMessage, FunctionContext context)
    {
        if (!_options.Triggers.ServiceBus.Enabled)
            return GenerateCorrelationId();

        string? correlationId = null;

        // Try to extract from Service Bus message properties
        if (_options.Triggers.ServiceBus.UseMessageCorrelationId)
        {
            correlationId = ExtractServiceBusCorrelationId(serviceBusMessage);
        }

        // Try application properties if not found
        if (string.IsNullOrWhiteSpace(correlationId) && _options.Triggers.ServiceBus.UseApplicationProperties)
        {
            correlationId = ExtractServiceBusApplicationProperty(serviceBusMessage, _options.Triggers.ServiceBus.ApplicationPropertyName);
        }

        // Generate if not found
        if (string.IsNullOrWhiteSpace(correlationId) && _options.AutoGenerate)
        {
            correlationId = GenerateCorrelationId();
        }

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            SetCorrelationId(correlationId);
            SetCorrelationContext(new CorrelationContext
            {
                CorrelationId = correlationId,
                TriggerType = "ServiceBus",
                Timestamp = DateTime.UtcNow,
                Source = context.FunctionDefinition.Name
            });
        }

        return correlationId ?? string.Empty;
    }

    public string InitializeForEventHubTrigger(object eventData, FunctionContext context)
    {
        if (!_options.Triggers.EventHub.Enabled)
            return GenerateCorrelationId();

        string? correlationId = null;

        // Try to extract from event properties
        if (_options.Triggers.EventHub.UseEventProperties)
        {
            correlationId = ExtractEventHubProperty(eventData, _options.Triggers.EventHub.EventPropertyName);
        }

        // Try parsing from event body if enabled
        if (string.IsNullOrWhiteSpace(correlationId) && _options.Triggers.EventHub.ParseFromEventBody)
        {
            var eventBody = ExtractEventHubBody(eventData);
            if (!string.IsNullOrWhiteSpace(eventBody))
            {
                correlationId = ExtractFromJsonMessage(eventBody, _options.Triggers.EventHub.EventBodyPropertyPath);
            }
        }

        // Generate if not found
        if (string.IsNullOrWhiteSpace(correlationId) && _options.AutoGenerate)
        {
            correlationId = GenerateCorrelationId();
        }

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            SetCorrelationId(correlationId);
            SetCorrelationContext(new CorrelationContext
            {
                CorrelationId = correlationId,
                TriggerType = "EventHub",
                Timestamp = DateTime.UtcNow,
                Source = context.FunctionDefinition.Name
            });
        }

        return correlationId ?? string.Empty;
    }

    public string InitializeForTimerTrigger(object timerInfo, FunctionContext context)
    {
        if (!_options.Triggers.Timer.Enabled)
            return GenerateCorrelationId();

        string correlationId = GenerateCorrelationId(); // Always generate for timer

        SetCorrelationId(correlationId);
        var correlationContext = new CorrelationContext
        {
            CorrelationId = correlationId,
            TriggerType = "Timer",
            Timestamp = DateTime.UtcNow,
            Source = context.FunctionDefinition.Name
        };

        if (_options.Triggers.Timer.IncludeScheduleInfo)
        {
            var scheduleInfo = ExtractTimerScheduleInfo(timerInfo);
            if (scheduleInfo != null)
            {
                correlationContext.AdditionalProperties["Schedule"] = scheduleInfo;
            }
        }

        SetCorrelationContext(correlationContext);
        return correlationId;
    }

    public string InitializeForBlobTrigger(object blobTriggerData, FunctionContext context)
    {
        if (!_options.Triggers.Blob.Enabled)
            return GenerateCorrelationId();

        string? correlationId = null;

        // Try to extract from blob metadata
        if (_options.Triggers.Blob.UseBlobMetadata)
        {
            correlationId = ExtractBlobMetadata(blobTriggerData, _options.Triggers.Blob.MetadataKeyName);
        }

        // Generate from blob path if enabled and not found
        if (string.IsNullOrWhiteSpace(correlationId) && _options.Triggers.Blob.GenerateFromBlobPath)
        {
            var blobPath = ExtractBlobPath(blobTriggerData);
            if (!string.IsNullOrWhiteSpace(blobPath))
            {
                correlationId = GenerateCorrelationIdFromPath(blobPath);
            }
        }

        // Generate if not found
        if (string.IsNullOrWhiteSpace(correlationId) && _options.AutoGenerate)
        {
            correlationId = GenerateCorrelationId();
        }

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            SetCorrelationId(correlationId);
            SetCorrelationContext(new CorrelationContext
            {
                CorrelationId = correlationId,
                TriggerType = "Blob",
                Timestamp = DateTime.UtcNow,
                Source = context.FunctionDefinition.Name,
                AdditionalProperties = { ["BlobPath"] = ExtractBlobPath(blobTriggerData) ?? "Unknown" }
            });
        }

        return correlationId ?? string.Empty;
    }

    public CorrelationContext GetCorrelationContext()
    {
        return _correlationContext.Value ?? new CorrelationContext
        {
            CorrelationId = CorrelationId,
            TriggerType = "Unknown",
            Timestamp = DateTime.UtcNow,
            Source = "Unknown"
        };
    }

    private void SetCorrelationContext(CorrelationContext context)
    {
        _correlationContext.Value = context;
    }

    private string? ExtractFromHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
    {
        var header = headers.FirstOrDefault(h => 
            string.Equals(h.Key, _options.HeaderName, StringComparison.OrdinalIgnoreCase));
        return header.Value?.FirstOrDefault();
    }

    private string? ExtractFromQueryParameters(Uri requestUri)
    {
        var query = requestUri.Query;
        if (string.IsNullOrEmpty(query)) return null;

        // Simple query parameter parsing (remove ? and split by &)
        var queryString = query.TrimStart('?');
        var pairs = queryString.Split('&');
        
        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=');
            if (keyValue.Length == 2 && 
                string.Equals(keyValue[0], _options.Triggers.Http.QueryParameterName, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(keyValue[1]);
            }
        }
        
        return null;
    }

    private string? ExtractFromJsonMessage(string jsonMessage, string propertyPath)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonMessage);
            if (document.RootElement.TryGetProperty(propertyPath, out var property))
            {
                return property.GetString();
            }
        }
        catch
        {
            // Ignore JSON parsing errors
        }
        return null;
    }

    private string? ExtractQueueName(FunctionContext context)
    {
        // Extract queue name from function context if available
        return context.BindingContext.BindingData.TryGetValue("QueueName", out var queueName) 
            ? queueName?.ToString() 
            : null;
    }

    private string? ExtractServiceBusCorrelationId(object serviceBusMessage)
    {
        // Use reflection to extract CorrelationId property from Service Bus message
        return ExtractProperty(serviceBusMessage, "CorrelationId")?.ToString();
    }

    private string? ExtractServiceBusApplicationProperty(object serviceBusMessage, string propertyName)
    {
        // Use reflection to extract from ApplicationProperties dictionary
        var appProps = ExtractProperty(serviceBusMessage, "ApplicationProperties");
        if (appProps is IDictionary<string, object> dict && dict.TryGetValue(propertyName, out var value))
        {
            return value?.ToString();
        }
        return null;
    }

    private string? ExtractEventHubProperty(object eventData, string propertyName)
    {
        // Use reflection to extract from Properties dictionary
        var props = ExtractProperty(eventData, "Properties");
        if (props is IDictionary<string, object> dict && dict.TryGetValue(propertyName, out var value))
        {
            return value?.ToString();
        }
        return null;
    }

    private string? ExtractEventHubBody(object eventData)
    {
        // Use reflection to extract body content
        var bodyData = ExtractProperty(eventData, "Body") ?? ExtractProperty(eventData, "Data");
        if (bodyData is byte[] bytes)
        {
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        return bodyData?.ToString();
    }

    private string? ExtractTimerScheduleInfo(object timerInfo)
    {
        // Extract schedule information from timer info
        return ExtractProperty(timerInfo, "Schedule")?.ToString();
    }

    private string? ExtractBlobMetadata(object blobTriggerData, string metadataKey)
    {
        // Use reflection to extract from Metadata dictionary
        var metadata = ExtractProperty(blobTriggerData, "Metadata");
        if (metadata is IDictionary<string, string> dict && dict.TryGetValue(metadataKey, out var value))
        {
            return value;
        }
        return null;
    }

    private string? ExtractBlobPath(object blobTriggerData)
    {
        // Extract blob path/name
        return ExtractProperty(blobTriggerData, "Name")?.ToString() ?? ExtractProperty(blobTriggerData, "Uri")?.ToString();
    }

    private string GenerateCorrelationIdFromPath(string path)
    {
        // Generate a deterministic correlation ID from blob path
        using var hash = System.Security.Cryptography.SHA256.Create();
        var hashBytes = hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(path));
        return Convert.ToHexString(hashBytes)[..16]; // Take first 16 characters
    }

    private object? ExtractProperty(object obj, string propertyName)
    {
        try
        {
            var property = obj.GetType().GetProperty(propertyName);
            return property?.GetValue(obj);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Correlation context information
/// </summary>
public class CorrelationContext
{
    public string CorrelationId { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalProperties { get; set; } = new();
}
