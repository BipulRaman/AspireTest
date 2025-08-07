using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace AspireApp1.CorrelationId;

/// <summary>
/// HTTP client message handler that automatically adds correlation ID to outgoing requests
/// </summary>
public class CorrelationIdHttpMessageHandler : DelegatingHandler
{
    private readonly ICorrelationIdService _correlationIdService;
    private readonly ILogger<CorrelationIdHttpMessageHandler> _logger;
    private readonly CorrelationIdOptions _options;

    public CorrelationIdHttpMessageHandler(
        ICorrelationIdService correlationIdService, 
        ILogger<CorrelationIdHttpMessageHandler> logger,
        IOptions<CorrelationIdOptions> options)
    {
        _correlationIdService = correlationIdService;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = _correlationIdService.CorrelationId;

        // Add correlation ID header if not already present (using configurable header name)
        if (!request.Headers.Contains(_options.CorrelationIdHeader) && !string.IsNullOrWhiteSpace(correlationId))
        {
            request.Headers.Add(_options.CorrelationIdHeader, correlationId);
            _logger.LogDebug("Added correlation ID {CorrelationId} to outgoing request to {Uri}", 
                correlationId, request.RequestUri);
        }

        // Add additional headers if they exist
        var capturedHeaders = _correlationIdService.CapturedHeaders;
        foreach (var header in capturedHeaders)
        {
            // Skip correlation ID as it's already handled above
            if (header.Key == _options.CorrelationIdHeader)
                continue;

            // Add header if not already present in the request
            if (!request.Headers.Contains(header.Key) && !string.IsNullOrWhiteSpace(header.Value))
            {
                request.Headers.Add(header.Key, header.Value);
                _logger.LogDebug("Added additional header {HeaderName}: {HeaderValue} to outgoing request to {Uri}", 
                    header.Key, header.Value, request.RequestUri);
            }
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
