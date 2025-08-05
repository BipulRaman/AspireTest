using Microsoft.Extensions.Logging;

namespace AspireApp1.CorrelationId;

/// <summary>
/// Logger provider that creates CorrelationIdLogger instances
/// </summary>
public class CorrelationIdLoggerProvider : ILoggerProvider
{
    private readonly ILoggerProvider _innerProvider;
    private readonly ICorrelationIdService _correlationIdService;

    public CorrelationIdLoggerProvider(ILoggerProvider innerProvider, ICorrelationIdService correlationIdService)
    {
        _innerProvider = innerProvider;
        _correlationIdService = correlationIdService;
    }

    public ILogger CreateLogger(string categoryName)
    {
        var innerLogger = _innerProvider.CreateLogger(categoryName);
        return new CorrelationIdLoggerWrapper(innerLogger, _correlationIdService);
    }

    public void Dispose()
    {
        _innerProvider?.Dispose();
    }

    private class CorrelationIdLoggerWrapper : ILogger
    {
        private readonly ILogger _logger;
        private readonly ICorrelationIdService _correlationIdService;

        public CorrelationIdLoggerWrapper(ILogger logger, ICorrelationIdService correlationIdService)
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

            var correlationId = _correlationIdService.CorrelationId;
            var originalMessage = formatter(state, exception);
            var messageWithCorrelationId = $"[CorrelationId: {correlationId}] {originalMessage}";

            _logger.Log(logLevel, eventId, messageWithCorrelationId, exception, (s, ex) => s);
        }
    }
}
