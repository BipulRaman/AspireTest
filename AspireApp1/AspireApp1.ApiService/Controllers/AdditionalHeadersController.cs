using Microsoft.AspNetCore.Mvc;
using AspireApp1.CorrelationId;

namespace AspireApp1.ApiService.Controllers;

/// <summary>
/// Controller demonstrating the additional headers feature alongside correlation ID
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AdditionalHeadersController : ControllerBase
{
    private readonly ILogger<AdditionalHeadersController> _logger;
    private readonly ICorrelationIdService _correlationIdService;

    public AdditionalHeadersController(
        ILogger<AdditionalHeadersController> logger,
        ICorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    /// <summary>
    /// Demonstrates automatic capturing of additional headers
    /// Send requests with headers like X-Event-Id, X-User-Id, X-Request-Source
    /// </summary>
    [HttpGet("demo")]
    public IActionResult GetDemo()
    {
        _logger.LogInformation("Processing demo request with automatic header capture");
        
        var response = new
        {
            Message = "Additional headers demo",
            Timestamp = DateTime.UtcNow,
            CapturedHeaders = _correlationIdService.CapturedHeaders,
            CorrelationId = _correlationIdService.CorrelationId,
            EventId = _correlationIdService.GetHeader("X-Event-Id"),
            UserId = _correlationIdService.GetHeader("X-User-Id"),
            RequestSource = _correlationIdService.GetHeader("X-Request-Source")
        };
        
        _logger.LogInformation("Demo completed. Captured headers: {CapturedHeaders}", 
            string.Join(", ", _correlationIdService.CapturedHeaders.Select(h => $"{h.Key}={h.Value}")));
        
        return Ok(response);
    }

    /// <summary>
    /// Demonstrates setting additional headers programmatically
    /// </summary>
    [HttpPost("set-headers")]
    public IActionResult SetAdditionalHeaders([FromBody] Dictionary<string, string> headers)
    {
        _logger.LogInformation("Setting additional headers programmatically");
        
        // Set additional headers programmatically
        _correlationIdService.SetAdditionalHeaders(headers);
        
        _logger.LogInformation("Additional headers set successfully");
        
        var response = new
        {
            Message = "Additional headers set programmatically",
            Timestamp = DateTime.UtcNow,
            AllCapturedHeaders = _correlationIdService.CapturedHeaders,
            CorrelationId = _correlationIdService.CorrelationId
        };
        
        return Ok(response);
    }

    /// <summary>
    /// Demonstrates header flow through multiple log statements
    /// </summary>
    [HttpGet("flow-demo/{eventType}")]
    public async Task<IActionResult> GetFlowDemo(string eventType)
    {
        _logger.LogInformation("Starting flow demo for event type: {EventType}", eventType);
        
        // Simulate some processing steps
        await ProcessStep1();
        await ProcessStep2();
        await ProcessStep3();
        
        _logger.LogInformation("Flow demo completed for event type: {EventType}", eventType);
        
        var response = new
        {
            Message = $"Flow demo completed for {eventType}",
            Timestamp = DateTime.UtcNow,
            CapturedHeaders = _correlationIdService.CapturedHeaders,
            Steps = new[] { "Step1", "Step2", "Step3" }
        };
        
        return Ok(response);
    }

    private async Task ProcessStep1()
    {
        _logger.LogInformation("Executing Step 1 - Data validation");
        await Task.Delay(50); // Simulate processing
        _logger.LogInformation("Step 1 completed successfully");
    }

    private async Task ProcessStep2()
    {
        _logger.LogInformation("Executing Step 2 - Business logic processing");
        await Task.Delay(100); // Simulate processing
        _logger.LogInformation("Step 2 completed successfully");
    }

    private async Task ProcessStep3()
    {
        _logger.LogInformation("Executing Step 3 - Response preparation");
        await Task.Delay(25); // Simulate processing
        _logger.LogInformation("Step 3 completed successfully");
    }
}
