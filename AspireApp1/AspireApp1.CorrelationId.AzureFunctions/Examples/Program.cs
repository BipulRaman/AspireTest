using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AspireApp1.CorrelationId.AzureFunctions;

namespace AspireApp1.CorrelationId.AzureFunctions.Examples;

/// <summary>
/// Example Program.cs for Azure Functions with additional headers configuration
/// </summary>
public class Program
{
    public static void Main()
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults()
            .ConfigureServices(services =>
            {
                // Basic setup - correlation ID only
                // services.AddCorrelationId();

                // Advanced setup - correlation ID with additional headers
                services.AddCorrelationId(options =>
                {
                    // Configure additional headers to capture alongside correlation ID
                    options.AdditionalHeaders.AddRange(new[]
                    {
                        "X-Event-Id",           // Custom event tracking header
                        "X-User-Id",            // User identifier for request tracking
                        "X-Request-Source",     // Source system identifier
                        "X-Tenant-Id",          // Multi-tenant identifier
                        "X-Session-Id"          // Session tracking
                    });

                    // Add captured headers to HTTP response headers for client tracking
                    options.AddAdditionalHeadersToResponse = true;

                    // Configure correlation ID header name (default: "X-Correlation-Id")
                    options.HeaderName = "X-Correlation-Id";

                    // Control auto-generation (default: true)
                    // When true: Automatically generates new correlation ID if request doesn't have one
                    // When false: Only tracks correlation ID if provided in request headers
                    options.AutoGenerate = true;

                    // Add correlation ID to HTTP response headers (default: true)
                    options.AddToResponseHeaders = true;

                    // Configure trigger-specific settings
                    options.Triggers.Http.Enabled = true;
                    options.Triggers.Http.UseQueryParameter = false; // Don't extract from query params
                    options.Triggers.Queue.Enabled = true;
                    options.Triggers.ServiceBus.Enabled = true;
                    options.Triggers.EventHub.Enabled = true;
                    options.Triggers.Timer.Enabled = true;
                    options.Triggers.Blob.Enabled = true;
                });

                // Alternative: With HTTP client integration
                // services.AddCorrelationIdWithHttpClient(options =>
                // {
                //     options.AdditionalHeaders.AddRange(new[]
                //     {
                //         "X-Event-Id",
                //         "X-User-Id",
                //         "X-Request-Source"
                //     });
                //     
                //     options.AddAdditionalHeadersToResponse = true;
                // });
            })
            .Build();

        host.Run();
    }
}

/// <summary>
/// Alternative configuration examples for different scenarios
/// </summary>
public static class ConfigurationExamples
{
    /// <summary>
    /// Basic configuration - correlation ID only
    /// </summary>
    public static void BasicSetup(IServiceCollection services)
    {
        services.AddCorrelationId();
    }

    /// <summary>
    /// Event tracking scenario - capture event and user information
    /// </summary>
    public static void EventTrackingSetup(IServiceCollection services)
    {
        services.AddCorrelationId(options =>
        {
            options.AdditionalHeaders.AddRange(new[]
            {
                "X-Event-Id",
                "X-Event-Type", 
                "X-User-Id",
                "X-Session-Id"
            });
            
            options.AddAdditionalHeadersToResponse = true;
        });
    }

    /// <summary>
    /// Multi-tenant scenario - capture tenant and organizational information
    /// </summary>
    public static void MultiTenantSetup(IServiceCollection services)
    {
        services.AddCorrelationId(options =>
        {
            options.AdditionalHeaders.AddRange(new[]
            {
                "X-Tenant-Id",
                "X-Organization-Id",
                "X-User-Id",
                "X-Request-Source"
            });
            
            options.AddAdditionalHeadersToResponse = true;
        });
    }

    /// <summary>
    /// Microservices scenario - with HTTP client integration
    /// </summary>
    public static void MicroservicesSetup(IServiceCollection services)
    {
        services.AddCorrelationIdWithHttpClient(options =>
        {
            options.AdditionalHeaders.AddRange(new[]
            {
                "X-Request-Id",
                "X-User-Id",
                "X-Service-Name",
                "X-Request-Source"
            });
            
            options.AddAdditionalHeadersToResponse = true;
            
            // Configure triggers for different Azure Functions scenarios
            options.Triggers.Http.Enabled = true;
            options.Triggers.Queue.Enabled = true;
            options.Triggers.ServiceBus.Enabled = true;
        });
    }

    /// <summary>
    /// Strict tracking scenario - only track when correlation ID is provided
    /// </summary>
    public static void StrictTrackingSetup(IServiceCollection services)
    {
        services.AddCorrelationId(options =>
        {
            options.AdditionalHeaders.AddRange(new[]
            {
                "X-Event-Id",
                "X-User-Id"
            });
            
            // Only track if correlation ID is provided, don't auto-generate
            options.AutoGenerate = false;
            options.AddAdditionalHeadersToResponse = false;
        });
    }
}
