namespace AspireApp1.CorrelationId.AzureFunctions;

/// <summary>
/// Configuration options for correlation ID behavior with different Azure Functions triggers
/// </summary>
public class CorrelationIdOptions
{
    /// <summary>
    /// Header name to use for HTTP triggers (default: X-Correlation-Id)
    /// </summary>
    public string CorrelationIdHeader { get; set; } = "X-Correlation-Id";

    /// <summary>
    /// List of additional headers to capture and track alongside correlation ID
    /// </summary>
    public List<string> AdditionalHeaders { get; set; } = new();

    /// <summary>
    /// Whether to automatically generate correlation ID if not present (default: true)
    /// </summary>
    public bool AutoGenerate { get; set; } = true;

    /// <summary>
    /// Whether to add correlation ID to HTTP response headers (default: true)
    /// </summary>
    public bool AddToResponseHeaders { get; set; } = true;

    /// <summary>
    /// Whether to add captured additional headers to HTTP response headers (default: false)
    /// </summary>
    public bool AddAdditionalHeadersToResponse { get; set; } = false;

    /// <summary>
    /// Whether to log function start/end automatically (default: true)
    /// </summary>
    public bool LogFunctionExecution { get; set; } = true;

    /// <summary>
    /// Trigger-specific configuration
    /// </summary>
    public TriggerConfiguration Triggers { get; set; } = new();
}

/// <summary>
/// Configuration for different Azure Functions trigger types
/// </summary>
public class TriggerConfiguration
{
    /// <summary>
    /// Configuration for HTTP triggers
    /// </summary>
    public HttpTriggerConfig Http { get; set; } = new();

    /// <summary>
    /// Configuration for Queue triggers
    /// </summary>
    public QueueTriggerConfig Queue { get; set; } = new();

    /// <summary>
    /// Configuration for Service Bus triggers
    /// </summary>
    public ServiceBusTriggerConfig ServiceBus { get; set; } = new();

    /// <summary>
    /// Configuration for Event Hub triggers
    /// </summary>
    public EventHubTriggerConfig EventHub { get; set; } = new();

    /// <summary>
    /// Configuration for Timer triggers
    /// </summary>
    public TimerTriggerConfig Timer { get; set; } = new();

    /// <summary>
    /// Configuration for Blob triggers
    /// </summary>
    public BlobTriggerConfig Blob { get; set; } = new();
}

/// <summary>
/// HTTP trigger specific configuration
/// </summary>
public class HttpTriggerConfig
{
    /// <summary>
    /// Whether HTTP trigger correlation is enabled (default: true)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to extract correlation ID from query parameters if header is missing (default: false)
    /// </summary>
    public bool UseQueryParameter { get; set; } = false;

    /// <summary>
    /// Query parameter name for correlation ID (default: correlationId)
    /// </summary>
    public string QueryParameterName { get; set; } = "correlationId";
}

/// <summary>
/// Queue trigger specific configuration
/// </summary>
public class QueueTriggerConfig
{
    /// <summary>
    /// Whether Queue trigger correlation is enabled (default: true)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to extract correlation ID from message properties (default: true)
    /// </summary>
    public bool UseMessageProperties { get; set; } = true;

    /// <summary>
    /// Property name to look for correlation ID in queue message (default: CorrelationId)
    /// </summary>
    public string MessagePropertyName { get; set; } = "CorrelationId";

    /// <summary>
    /// Whether to try parsing correlation ID from JSON message body (default: false)
    /// </summary>
    public bool ParseFromMessageBody { get; set; } = false;

    /// <summary>
    /// JSON property path for correlation ID in message body (default: correlationId)
    /// </summary>
    public string MessageBodyPropertyPath { get; set; } = "correlationId";
}

/// <summary>
/// Service Bus trigger specific configuration
/// </summary>
public class ServiceBusTriggerConfig
{
    /// <summary>
    /// Whether Service Bus trigger correlation is enabled (default: true)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to use Service Bus message CorrelationId property (default: true)
    /// </summary>
    public bool UseMessageCorrelationId { get; set; } = true;

    /// <summary>
    /// Whether to extract from custom application properties (default: true)
    /// </summary>
    public bool UseApplicationProperties { get; set; } = true;

    /// <summary>
    /// Application property name for correlation ID (default: CorrelationId)
    /// </summary>
    public string ApplicationPropertyName { get; set; } = "CorrelationId";
}

/// <summary>
/// Event Hub trigger specific configuration
/// </summary>
public class EventHubTriggerConfig
{
    /// <summary>
    /// Whether Event Hub trigger correlation is enabled (default: true)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to extract correlation ID from event properties (default: true)
    /// </summary>
    public bool UseEventProperties { get; set; } = true;

    /// <summary>
    /// Property name for correlation ID in event properties (default: CorrelationId)
    /// </summary>
    public string EventPropertyName { get; set; } = "CorrelationId";

    /// <summary>
    /// Whether to parse correlation ID from event body JSON (default: false)
    /// </summary>
    public bool ParseFromEventBody { get; set; } = false;

    /// <summary>
    /// JSON property path for correlation ID in event body (default: correlationId)
    /// </summary>
    public string EventBodyPropertyPath { get; set; } = "correlationId";
}

/// <summary>
/// Timer trigger specific configuration
/// </summary>
public class TimerTriggerConfig
{
    /// <summary>
    /// Whether Timer trigger correlation is enabled (default: true)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to generate a new correlation ID for each timer execution (default: true)
    /// </summary>
    public bool GenerateForEachExecution { get; set; } = true;

    /// <summary>
    /// Whether to include timer schedule information in correlation context (default: true)
    /// </summary>
    public bool IncludeScheduleInfo { get; set; } = true;
}

/// <summary>
/// Blob trigger specific configuration
/// </summary>
public class BlobTriggerConfig
{
    /// <summary>
    /// Whether Blob trigger correlation is enabled (default: true)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to extract correlation ID from blob metadata (default: true)
    /// </summary>
    public bool UseBlobMetadata { get; set; } = true;

    /// <summary>
    /// Metadata key name for correlation ID (default: CorrelationId)
    /// </summary>
    public string MetadataKeyName { get; set; } = "CorrelationId";

    /// <summary>
    /// Whether to generate correlation ID from blob name/path (default: false)
    /// </summary>
    public bool GenerateFromBlobPath { get; set; } = false;
}
