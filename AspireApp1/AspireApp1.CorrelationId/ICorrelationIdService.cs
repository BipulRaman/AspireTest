namespace AspireApp1.CorrelationId;

/// <summary>
/// Service for managing correlation IDs and additional headers across the application
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
    /// Gets a specific captured header value
    /// </summary>
    string? GetHeader(string headerName);

    /// <summary>
    /// Sets the correlation ID for the current context
    /// </summary>
    void SetCorrelationId(string correlationId);

    /// <summary>
    /// Sets additional headers for the current context
    /// </summary>
    void SetAdditionalHeaders(Dictionary<string, string> headers);

    /// <summary>
    /// Generates a new correlation ID
    /// </summary>
    string GenerateCorrelationId();
}
