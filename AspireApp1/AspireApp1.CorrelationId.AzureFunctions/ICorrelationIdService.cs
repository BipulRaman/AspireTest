namespace AspireApp1.CorrelationId.AzureFunctions;

/// <summary>
/// Service for managing correlation IDs across Azure Functions
/// Uses AsyncLocal for thread safety within function execution context
/// </summary>
public interface ICorrelationIdService
{
    /// <summary>
    /// Gets the current correlation ID
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Sets the correlation ID for the current function execution context
    /// </summary>
    void SetCorrelationId(string correlationId);

    /// <summary>
    /// Generates a new correlation ID
    /// </summary>
    string GenerateCorrelationId();

    /// <summary>
    /// Gets or creates a correlation ID from HTTP headers
    /// </summary>
    string GetOrCreateFromHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers);
}
