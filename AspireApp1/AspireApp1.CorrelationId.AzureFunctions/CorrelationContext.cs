namespace AspireApp1.CorrelationId.AzureFunctions;

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
