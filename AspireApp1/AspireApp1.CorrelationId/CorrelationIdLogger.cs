using Microsoft.Extensions.Logging;

namespace AspireApp1.CorrelationId;

/// <summary>
/// Custom logger that automatically adds correlation ID to all log entries
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

        // Create a new state with correlation ID
        var correlationId = _correlationIdService.CorrelationId;
        var originalMessage = formatter(state, exception);
        var messageWithCorrelationId = $"[CorrelationId: {correlationId}] {originalMessage}";

        _logger.Log(logLevel, eventId, messageWithCorrelationId, exception, (s, ex) => s);
    }
}
