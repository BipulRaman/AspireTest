namespace AspireApp1.CorrelationId.AzureFunctions;

/// <summary>
/// Azure Functions implementation for managing correlation IDs and additional headers using AsyncLocal for thread safety
/// </summary>
public class CorrelationIdService : ICorrelationIdService
{
    private static readonly AsyncLocal<string> _correlationId = new();
    private static readonly AsyncLocal<Dictionary<string, string>> _additionalHeaders = new();
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public string CorrelationId => _correlationId.Value ?? GenerateCorrelationId();

    public Dictionary<string, string> CapturedHeaders
    {
        get
        {
            var headers = new Dictionary<string, string>();
            
            // Add correlation ID
            var correlationId = _correlationId.Value;
            if (!string.IsNullOrEmpty(correlationId))
            {
                headers[CorrelationIdHeader] = correlationId;
            }
            
            // Add additional headers
            var additionalHeaders = _additionalHeaders.Value;
            if (additionalHeaders != null)
            {
                foreach (var header in additionalHeaders)
                {
                    headers[header.Key] = header.Value;
                }
            }
            
            return headers;
        }
    }

    public void SetCorrelationId(string correlationId)
    {
        _correlationId.Value = correlationId;
    }

    public string? GetHeader(string headerName)
    {
        if (string.Equals(headerName, CorrelationIdHeader, StringComparison.OrdinalIgnoreCase))
        {
            return _correlationId.Value;
        }

        var additionalHeaders = _additionalHeaders.Value;
        if (additionalHeaders != null && additionalHeaders.TryGetValue(headerName, out var value))
        {
            return value;
        }

        return null;
    }

    public void SetAdditionalHeaders(Dictionary<string, string> headers)
    {
        _additionalHeaders.Value = new Dictionary<string, string>(headers);
    }

    public string GenerateCorrelationId()
    {
        var newCorrelationId = Guid.NewGuid().ToString(); // Full GUID correlation ID
        _correlationId.Value = newCorrelationId;
        return newCorrelationId;
    }

    public string GetOrCreateFromHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers, IEnumerable<string>? additionalHeadersToCapture = null)
    {
        var headerDict = headers.ToDictionary(
            h => h.Key,
            h => h.Value.FirstOrDefault() ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);

        // Look for correlation ID header
        var correlationId = headerDict.GetValueOrDefault(CorrelationIdHeader);
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            SetCorrelationId(correlationId);
        }
        else
        {
            correlationId = GenerateCorrelationId();
        }

        // Capture additional headers if specified
        if (additionalHeadersToCapture != null)
        {
            var capturedAdditionalHeaders = new Dictionary<string, string>();
            foreach (var headerName in additionalHeadersToCapture)
            {
                if (headerDict.TryGetValue(headerName, out var headerValue) && !string.IsNullOrEmpty(headerValue))
                {
                    capturedAdditionalHeaders[headerName] = headerValue;
                }
            }

            if (capturedAdditionalHeaders.Count > 0)
            {
                SetAdditionalHeaders(capturedAdditionalHeaders);
            }
        }

        return correlationId;
    }
}
