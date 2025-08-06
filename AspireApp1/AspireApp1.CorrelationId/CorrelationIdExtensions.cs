using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;

namespace AspireApp1.CorrelationId;

/// <summary>
/// Extension methods for configuring correlation ID functionality
/// </summary>
public static class CorrelationIdExtensions
{
    /// <summary>
    /// Adds correlation ID services to the dependency injection container with default configuration
    /// </summary>
    public static IServiceCollection AddCorrelationId(this IServiceCollection services)
    {
        return services.AddCorrelationId(_ => { });
    }

    /// <summary>
    /// Adds correlation ID services to the dependency injection container with configuration
    /// </summary>
    public static IServiceCollection AddCorrelationId(this IServiceCollection services, Action<CorrelationIdOptions> configureOptions)
    {
        services.Configure(configureOptions);
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
    /// Adds correlation ID services with HTTP client integration
    /// </summary>
    public static IServiceCollection AddCorrelationIdWithHttpClient(this IServiceCollection services, Action<CorrelationIdOptions>? configureOptions = null)
    {
        // Add basic correlation ID services
        services.AddCorrelationId(configureOptions ?? (_ => { }));

        // Register the HTTP message handler
        services.AddTransient<CorrelationIdHttpMessageHandler>();

        // Configure default HTTP client with correlation ID support
        services.AddHttpClient(CorrelationIdHttpClientNames.Default)
            .AddHttpMessageHandler<CorrelationIdHttpMessageHandler>();

        // Configure named HTTP clients for different scenarios
        services.AddHttpClient(CorrelationIdHttpClientNames.ExternalApi)
            .AddHttpMessageHandler<CorrelationIdHttpMessageHandler>();

        services.AddHttpClient(CorrelationIdHttpClientNames.InternalService)
            .AddHttpMessageHandler<CorrelationIdHttpMessageHandler>();

        // Register the correlated HTTP client service
        services.AddHttpClient<ICorrelatedHttpClient, CorrelatedHttpClient>(CorrelationIdHttpClientNames.Default)
            .AddHttpMessageHandler<CorrelationIdHttpMessageHandler>();

        return services;
    }

    /// <summary>
    /// Adds correlation ID message handler to a specific HTTP client
    /// </summary>
    public static IHttpClientBuilder AddCorrelationId(this IHttpClientBuilder builder)
    {
        return builder.AddHttpMessageHandler<CorrelationIdHttpMessageHandler>();
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

            var capturedHeaders = _correlationIdService.CapturedHeaders;
            
            // Automatically create structured logging state with all captured headers
            var enhancedState = new CorrelationEnhancedState<TState>
            {
                OriginalState = state,
                CapturedHeaders = capturedHeaders
            };

            // Add all captured headers to the message for readability
            var originalMessage = formatter(state, exception);
            var headerInfo = string.Join(", ", capturedHeaders.Select(h => $"{h.Key}: {h.Value}"));
            var messageWithHeaders = $"[{headerInfo}] {originalMessage}";

            _logger.Log(logLevel, eventId, enhancedState, exception, (enhancedState, ex) => messageWithHeaders);
        }
    }
}

/// <summary>
/// Enhanced state object that automatically includes all captured headers as structured properties
/// </summary>
internal class CorrelationEnhancedState<TState> : IReadOnlyList<KeyValuePair<string, object>>
{
    public required TState OriginalState { get; init; }
    public required Dictionary<string, string> CapturedHeaders { get; init; }

    private List<KeyValuePair<string, object>>? _properties;

    private List<KeyValuePair<string, object>> Properties
    {
        get
        {
            if (_properties == null)
            {
                _properties = new List<KeyValuePair<string, object>>();
                
                // Add all captured headers as structured properties
                foreach (var header in CapturedHeaders)
                {
                    _properties.Add(new KeyValuePair<string, object>(header.Key, header.Value));
                }

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
