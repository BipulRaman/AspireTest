namespace AspireApp1.CorrelationId;

/// <summary>
/// Service implementation for managing correlation IDs using AsyncLocal for thread safety
/// </summary>
public class CorrelationIdService : ICorrelationIdService
{
    private static readonly AsyncLocal<string> _correlationId = new();

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
}
