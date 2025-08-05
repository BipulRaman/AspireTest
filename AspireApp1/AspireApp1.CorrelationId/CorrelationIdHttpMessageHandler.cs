using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace AspireApp1.CorrelationId;

/// <summary>
/// HTTP client message handler that automatically adds correlation ID to outgoing requests
/// </summary>
public class CorrelationIdHttpMessageHandler : DelegatingHandler
{
    private readonly ICorrelationIdService _correlationIdService;
    private readonly ILogger<CorrelationIdHttpMessageHandler> _logger;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public CorrelationIdHttpMessageHandler(ICorrelationIdService correlationIdService, ILogger<CorrelationIdHttpMessageHandler> logger)
    {
        _correlationIdService = correlationIdService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = _correlationIdService.CorrelationId;

        // Add correlation ID header if not already present
        if (!request.Headers.Contains(CorrelationIdHeader) && !string.IsNullOrWhiteSpace(correlationId))
        {
            request.Headers.Add(CorrelationIdHeader, correlationId);
            _logger.LogDebug("Added correlation ID {CorrelationId} to outgoing request to {Uri}", 
                correlationId, request.RequestUri);
        }

        // Log the outgoing request with correlation context
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["OutgoingRequest"] = true,
            ["RequestUri"] = request.RequestUri?.ToString() ?? "Unknown",
            ["HttpMethod"] = request.Method.ToString()
        });

        _logger.LogInformation("Sending HTTP {Method} request to {Uri}", 
            request.Method, request.RequestUri);

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            
            _logger.LogInformation("Received HTTP {StatusCode} response from {Uri}", 
                (int)response.StatusCode, request.RequestUri);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP request to {Uri} failed", request.RequestUri);
            throw;
        }
    }
}

/// <summary>
/// Named HTTP client configurations for correlation ID integration
/// </summary>
public static class CorrelationIdHttpClientNames
{
    /// <summary>
    /// Default HTTP client with correlation ID support
    /// </summary>
    public const string Default = "CorrelationIdClient";

    /// <summary>
    /// HTTP client for external API calls with correlation ID
    /// </summary>
    public const string ExternalApi = "ExternalApiClient";

    /// <summary>
    /// HTTP client for internal service calls with correlation ID
    /// </summary>
    public const string InternalService = "InternalServiceClient";
}
