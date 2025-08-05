using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AspireApp1.CorrelationId;

/// <summary>
/// Extension methods for configuring correlation ID functionality
/// </summary>
public static class CorrelationIdExtensions
{
    /// <summary>
    /// Adds correlation ID services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddCorrelationId(this IServiceCollection services)
    {
        services.AddSingleton<ICorrelationIdService, CorrelationIdService>();
        
        // Replace the default logger factory to include correlation ID
        services.Decorate<ILoggerFactory>((factory, provider) =>
        {
            var correlationIdService = provider.GetRequiredService<ICorrelationIdService>();
            return new CorrelationIdLoggerFactory(factory, correlationIdService);
        });

        return services;
    }

    /// <summary>
    /// Adds correlation ID middleware to the request pipeline
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }

    /// <summary>
    /// Extension method to decorate services (simple implementation)
    /// </summary>
    private static IServiceCollection Decorate<TInterface>(this IServiceCollection services, Func<TInterface, IServiceProvider, TInterface> decorator)
        where TInterface : class
    {
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(TInterface));
        if (descriptor != null)
        {
            services.Remove(descriptor);
            
            if (descriptor.ImplementationInstance != null)
            {
                services.AddSingleton<TInterface>(provider => decorator((TInterface)descriptor.ImplementationInstance, provider));
            }
            else if (descriptor.ImplementationFactory != null)
            {
                services.AddSingleton<TInterface>(provider => decorator((TInterface)descriptor.ImplementationFactory(provider), provider));
            }
            else if (descriptor.ImplementationType != null)
            {
                services.AddSingleton<TInterface>(provider => decorator((TInterface)ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType), provider));
            }
        }

        return services;
    }
}

/// <summary>
/// Custom logger factory that wraps loggers with correlation ID functionality
/// </summary>
internal class CorrelationIdLoggerFactory : ILoggerFactory
{
    private readonly ILoggerFactory _innerFactory;
    private readonly ICorrelationIdService _correlationIdService;

    public CorrelationIdLoggerFactory(ILoggerFactory innerFactory, ICorrelationIdService correlationIdService)
    {
        _innerFactory = innerFactory;
        _correlationIdService = correlationIdService;
    }

    public ILogger CreateLogger(string categoryName)
    {
        var innerLogger = _innerFactory.CreateLogger(categoryName);
        return new CorrelationIdLoggerWrapper(innerLogger, _correlationIdService);
    }

    public void AddProvider(ILoggerProvider provider)
    {
        _innerFactory.AddProvider(provider);
    }

    public void Dispose()
    {
        _innerFactory.Dispose();
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
            
            // Automatically create structured logging state with correlation ID properties
            var enhancedState = new CorrelationEnhancedState<TState>
            {
                OriginalState = state,
                CorrelationId = correlationId
            };

            // Add correlation ID to the message for readability and structured properties automatically
            var originalMessage = formatter(state, exception);
            var messageWithCorrelationId = $"[CorrelationId: {correlationId}] {originalMessage}";

            _logger.Log(logLevel, eventId, enhancedState, exception, (enhancedState, ex) => messageWithCorrelationId);
        }
    }
}

/// <summary>
/// Enhanced state object that automatically includes correlation ID as structured properties
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
