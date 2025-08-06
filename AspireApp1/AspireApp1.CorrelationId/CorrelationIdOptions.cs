namespace AspireApp1.CorrelationId;

/// <summary>
/// Configuration options for correlation ID and additional headers
/// </summary>
public class CorrelationIdOptions
{
    /// <summary>
    /// Name of the correlation ID header (default: X-Correlation-Id)
    /// </summary>
    public string CorrelationIdHeader { get; set; } = "X-Correlation-Id";

    /// <summary>
    /// Additional headers to capture and include in logs alongside correlation ID
    /// </summary>
    public List<string> AdditionalHeaders { get; set; } = new();

    /// <summary>
    /// Whether to automatically generate correlation ID if not present in request headers
    /// </summary>
    public bool AutoGenerate { get; set; } = true;

    /// <summary>
    /// Whether to add correlation ID to response headers
    /// </summary>
    public bool AddToResponseHeaders { get; set; } = true;

    /// <summary>
    /// Whether to add additional headers to response headers
    /// </summary>
    public bool AddAdditionalHeadersToResponse { get; set; } = false;

    /// <summary>
    /// Prefix for log messages (default: empty - correlation context added via structured logging)
    /// </summary>
    public string LogPrefix { get; set; } = string.Empty;
}
