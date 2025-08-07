using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Http;

namespace AspireApp1.CorrelationId.AzureFunctions;

/// <summary>
/// Extension methods for configuring correlation ID functionality in Azure Functions
/// </summary>
public static class CorrelationIdExtensions
{
    /// <summary>
    /// Adds correlation ID services to Azure Functions host builder with automatic logger wrapping
    /// This automatically includes correlation ID as custom properties in all log entries
    /// </summary>
    public static IServiceCollection AddCorrelationId(this IServiceCollection services)
    {
        return services.AddCorrelationId(_ => { });
    }

    /// <summary>
    /// Adds enhanced correlation ID services with multi-trigger support and configuration options
    /// </summary>
    public static IServiceCollection AddCorrelationId(this IServiceCollection services, Action<CorrelationIdOptions> configure)
    {
        // Configure options
        services.Configure(configure);

        // Register enhanced service
        services.AddSingleton<ICorrelationIdService, EnhancedCorrelationIdService>();
        services.AddSingleton<IEnhancedCorrelationIdService, EnhancedCorrelationIdService>();
        
        // Automatically decorate logger factory to wrap loggers with correlation ID functionality
        services.Decorate<ILoggerFactory>((factory, provider) =>
        {
            var correlationIdService = provider.GetRequiredService<ICorrelationIdService>();
            return new CorrelationIdLoggerFactory(factory, correlationIdService);
        });

        return services;
    }

    /// <summary>
    /// Adds correlation ID services with HTTP client integration for Azure Functions
    /// </summary>
    public static IServiceCollection AddCorrelationIdWithHttpClient(this IServiceCollection services, Action<CorrelationIdOptions>? configure = null)
    {
        // Add enhanced correlation ID services
        services.AddCorrelationId(configure ?? (_ => { }));

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

        services.AddHttpClient(CorrelationIdHttpClientNames.AzureService)
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
    /// Extension method to decorate services (simple implementation for Azure Functions)
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
/// Custom logger factory for Azure Functions that wraps loggers with correlation ID functionality
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
        return new CorrelationIdLogger(innerLogger, _correlationIdService);
    }

    public void AddProvider(ILoggerProvider provider)
    {
        _innerFactory.AddProvider(provider);
    }

    public void Dispose()
    {
        _innerFactory.Dispose();
    }
}
