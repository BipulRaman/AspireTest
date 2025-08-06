namespace AspireApp1.CorrelationId;

/// <summary>
/// Service implementation for managing correlation IDs and additional headers using AsyncLocal for thread safety
/// </summary>
public class CorrelationIdService : ICorrelationIdService
{
    private static readonly AsyncLocal<string> _correlationId = new();
    private static readonly AsyncLocal<Dictionary<string, string>> _additionalHeaders = new();

    public string CorrelationId => _correlationId.Value ?? GenerateCorrelationId();

    public Dictionary<string, string> CapturedHeaders
    {
        get
        {
            var headers = new Dictionary<string, string>
            {
                ["CorrelationId"] = CorrelationId
            };

            if (_additionalHeaders.Value != null)
            {
                foreach (var header in _additionalHeaders.Value)
                {
                    headers[header.Key] = header.Value;
                }
            }

            return headers;
        }
    }

    public string? GetHeader(string headerName)
    {
        if (string.Equals(headerName, "CorrelationId", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(headerName, "X-Correlation-Id", StringComparison.OrdinalIgnoreCase))
        {
            return CorrelationId;
        }

        return _additionalHeaders.Value?.GetValueOrDefault(headerName);
    }

    public void SetCorrelationId(string correlationId)
    {
        _correlationId.Value = correlationId;
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
}
