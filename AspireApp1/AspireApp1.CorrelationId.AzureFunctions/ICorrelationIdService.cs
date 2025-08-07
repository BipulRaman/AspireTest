namespace AspireApp1.CorrelationId.AzureFunctions;

/// <summary>
/// Service for managing correlation IDs and additional headers across Azure Functions
/// Uses AsyncLocal for thread safety within function execution context
/// </summary>
public interface ICorrelationIdService
{
    /// <summary>
    /// Gets the current correlation ID
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Gets all captured headers (correlation ID + additional headers)
    /// </summary>
    Dictionary<string, string> CapturedHeaders { get; }

    /// <summary>
    /// Sets the correlation ID for the current function execution context
    /// </summary>
    void SetCorrelationId(string correlationId);

    /// <summary>
    /// Gets a specific header value by name
    /// </summary>
    string? GetHeader(string headerName);

    /// <summary>
    /// Sets additional headers for the current function execution context
    /// </summary>
    void SetAdditionalHeaders(Dictionary<string, string> headers);

    /// <summary>
    /// Generates a new correlation ID
    /// </summary>
    string GenerateCorrelationId();

    /// <summary>
    /// Gets or creates a correlation ID from HTTP headers and captures additional headers
    /// </summary>
    string GetOrCreateFromHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers, IEnumerable<string>? additionalHeadersToCapture = null);
}
