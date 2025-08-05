using Microsoft.Extensions.Logging;

namespace AspireApp1.CorrelationId.AzureFunctions;

/// <summary>
/// Azure Functions logger wrapper that automatically adds correlation ID to all log entries
/// </summary>
public class CorrelationIdLogger : ILogger
{
    private readonly ILogger _logger;
    private readonly ICorrelationIdService _correlationIdService;

    public CorrelationIdLogger(ILogger logger, ICorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        // Create a scope with correlation ID included
        var correlationId = _correlationIdService.CorrelationId;
        var scopeState = new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        };

        if (state is IEnumerable<KeyValuePair<string, object>> originalScope)
        {
            foreach (var item in originalScope)
            {
                scopeState[item.Key] = item.Value;
            }
        }

        return _logger.BeginScope(scopeState);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _logger.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var correlationId = _correlationIdService.CorrelationId;
        
        // Create enhanced state with correlation ID for structured logging
        var enhancedState = new CorrelationEnhancedState<TState>
        {
            OriginalState = state,
            CorrelationId = correlationId
        };

        // Add correlation ID to message for readability
        var originalMessage = formatter(state, exception);
        var messageWithCorrelationId = $"[CorrelationId: {correlationId}] {originalMessage}";

        _logger.Log(logLevel, eventId, enhancedState, exception, (enhancedState, ex) => messageWithCorrelationId);
    }
}

/// <summary>
/// Enhanced state object that includes correlation ID as structured properties for Azure Functions logging
/// </summary>
internal class CorrelationEnhancedState<TState> : IReadOnlyList<KeyValuePair<string, object>>
{
    public required TState OriginalState { get; init; }
    public required string CorrelationId { get; init; }

    private List<KeyValuePair<string, object>>? _properties;

    private List<KeyValuePair<string, object>> Properties
    {
        get
        {
            if (_properties == null)
            {
                _properties = new List<KeyValuePair<string, object>>
                {
                    new("CorrelationId", CorrelationId)
                };

                // Include original state properties if it implements IReadOnlyList<KeyValuePair<string, object>>
                if (OriginalState is IReadOnlyList<KeyValuePair<string, object>> originalProperties)
                {
                    _properties.AddRange(originalProperties);
                }
            }
            return _properties;
        }
    }

    public KeyValuePair<string, object> this[int index] => Properties[index];
    public int Count => Properties.Count;

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => Properties.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString()
    {
        return OriginalState?.ToString() ?? string.Empty;
    }
}
