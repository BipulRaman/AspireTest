namespace AspireApp1.CorrelationId.AzureFunctions;

/// <summary>
/// Azure Functions implementation for managing correlation IDs using AsyncLocal for thread safety
/// </summary>
public class CorrelationIdService : ICorrelationIdService
{
    private static readonly AsyncLocal<string> _correlationId = new();
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public string CorrelationId => _correlationId.Value ?? GenerateCorrelationId();

    public void SetCorrelationId(string correlationId)
    {
        _correlationId.Value = correlationId;
    }

    public string GenerateCorrelationId()
    {
        var newCorrelationId = Guid.NewGuid().ToString(); // Full GUID correlation ID
        _correlationId.Value = newCorrelationId;
        return newCorrelationId;
    }

    public string GetOrCreateFromHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
    {
        // Look for X-Correlation-Id header (case-insensitive)
        var correlationHeader = headers.FirstOrDefault(h => 
            string.Equals(h.Key, CorrelationIdHeader, StringComparison.OrdinalIgnoreCase));

        if (correlationHeader.Value != null)
        {
            var headerValue = correlationHeader.Value.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerValue))
            {
                SetCorrelationId(headerValue);
                return headerValue;
            }
        }

        // Generate new correlation ID if not found in headers
        return GenerateCorrelationId();
    }
}
