using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AspireApp1.CorrelationId;

/// <summary>
/// Middleware to handle correlation ID and additional headers for all incoming requests
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICorrelationIdService _correlationIdService;
    private readonly CorrelationIdOptions _options;

    public CorrelationIdMiddleware(
        RequestDelegate next, 
        ICorrelationIdService correlationIdService,
        IOptions<CorrelationIdOptions> options)
    {
        _next = next;
        _correlationIdService = correlationIdService;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId;

        // Check if correlation ID exists in request headers
        if (context.Request.Headers.TryGetValue(_options.CorrelationIdHeader, out var headerValue) && 
            !string.IsNullOrWhiteSpace(headerValue.ToString()))
        {
            correlationId = headerValue.ToString();
        }
        else if (_options.AutoGenerate)
        {
            // Generate new correlation ID if not present and auto-generation is enabled
            correlationId = _correlationIdService.GenerateCorrelationId();
        }
        else
        {
            correlationId = string.Empty;
        }

        // Set correlation ID in service
        if (!string.IsNullOrEmpty(correlationId))
        {
            _correlationIdService.SetCorrelationId(correlationId);
        }

        // Capture additional headers
        var additionalHeaders = new Dictionary<string, string>();
        foreach (var headerName in _options.AdditionalHeaders)
        {
            if (context.Request.Headers.TryGetValue(headerName, out var additionalHeaderValue) &&
                !string.IsNullOrWhiteSpace(additionalHeaderValue.ToString()))
            {
                additionalHeaders[headerName] = additionalHeaderValue.ToString();
            }
        }

        // Set additional headers in service
        if (additionalHeaders.Any())
        {
            _correlationIdService.SetAdditionalHeaders(additionalHeaders);
        }

        // Add correlation ID to response headers
        if (_options.AddToResponseHeaders && !string.IsNullOrEmpty(correlationId))
        {
            context.Response.Headers.Append(_options.CorrelationIdHeader, correlationId);
        }

        // Add additional headers to response headers if configured
        if (_options.AddAdditionalHeadersToResponse)
        {
            foreach (var header in additionalHeaders)
            {
                context.Response.Headers.Append(header.Key, header.Value);
            }
        }

        // Continue with the request pipeline
        await _next(context);
    }
}
