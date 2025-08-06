using Microsoft.Extensions.Logging;

namespace AspireApp1.CorrelationId;

/// <summary>
/// Custom logger that automatically adds correlation ID and additional headers to all log entries
/// </summary>
public class CorrelationIdLogger<T> : ILogger<T>
{
    private readonly ILogger<T> _logger;
    private readonly ICorrelationIdService _correlationIdService;

    public CorrelationIdLogger(ILogger<T> logger, ICorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        // Create scope with all captured headers for structured logging
        var capturedHeaders = _correlationIdService.CapturedHeaders;
        
        if (capturedHeaders.Any())
        {
            // Begin scope with all headers as structured properties
            return _logger.BeginScope(capturedHeaders);
        }
        
        return _logger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _logger.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var capturedHeaders = _correlationIdService.CapturedHeaders;
        var originalMessage = formatter(state, exception);
        
        // Build header context for message prefix
        var headerContext = string.Join(", ", capturedHeaders.Select(h => $"{h.Key}: {h.Value}"));
        var messageWithHeaders = $"[{headerContext}] {originalMessage}";

        // Use structured logging scope to include all headers as searchable properties
        using var scope = _logger.BeginScope(capturedHeaders);
        _logger.Log(logLevel, eventId, messageWithHeaders, exception, (s, ex) => s);
    }
}
