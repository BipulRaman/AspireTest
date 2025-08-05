using Microsoft.AspNetCore.Http;

namespace AspireApp1.CorrelationId;

/// <summary>
/// Middleware to handle X-Correlation-Id header for all incoming requests
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICorrelationIdService _correlationIdService;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next, ICorrelationIdService correlationIdService)
    {
        _next = next;
        _correlationIdService = correlationIdService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId;

        // Check if correlation ID exists in request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue) && 
            !string.IsNullOrWhiteSpace(headerValue.ToString()))
        {
            correlationId = headerValue.ToString();
        }
        else
        {
            // Generate new correlation ID if not present
            correlationId = _correlationIdService.GenerateCorrelationId();
        }

        // Set correlation ID in service
        _correlationIdService.SetCorrelationId(correlationId);

        // Add correlation ID to response headers
        context.Response.Headers.Append(CorrelationIdHeader, correlationId);

        // Continue with the request pipeline
        await _next(context);
    }
}
