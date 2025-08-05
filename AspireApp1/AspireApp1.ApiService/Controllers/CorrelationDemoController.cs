using AspireApp1.CorrelationId;
using Microsoft.AspNetCore.Mvc;

namespace AspireApp1.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CorrelationDemoController : ControllerBase
{
    private readonly ILogger<CorrelationDemoController> _logger;
    private readonly ICorrelationIdService _correlationIdService;

    public CorrelationDemoController(ILogger<CorrelationDemoController> logger, ICorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    /// <summary>
    /// Demonstrates basic correlation ID tracking
    /// </summary>
    [HttpGet("basic")]
    public ActionResult<CorrelationDemo> GetBasic()
    {
        _logger.LogInformation("Basic correlation demo started");
        
        var result = CorrelationIdHelper.ExecuteWithCorrelationId(_correlationIdService, () =>
        {
            _logger.LogDebug("Processing basic correlation demo");
            
            return new CorrelationDemo
            {
                Message = "Basic correlation ID demo",
                CorrelationId = _correlationIdService.CorrelationId,
                Timestamp = DateTime.UtcNow,
                RequestId = HttpContext.TraceIdentifier
            };
        });

        _logger.LogInformation("Basic correlation demo completed");
        return Ok(result);
    }

    /// <summary>
    /// Demonstrates async operations with correlation ID
    /// </summary>
    [HttpGet("async")]
    public async Task<ActionResult<CorrelationDemo>> GetAsync()
    {
        _logger.LogInformation("Async correlation demo started");
        
        var result = await CorrelationIdHelper.ExecuteWithCorrelationIdAsync(_correlationIdService, async () =>
        {
            _logger.LogDebug("Processing async correlation demo");
            
            // Simulate async work
            await Task.Delay(100);
            _logger.LogDebug("Async delay completed");
            
            // Call another async method
            var additionalData = await ProcessAdditionalDataAsync();
            
            return new CorrelationDemo
            {
                Message = "Async correlation ID demo",
                CorrelationId = _correlationIdService.CorrelationId,
                Timestamp = DateTime.UtcNow,
                RequestId = HttpContext.TraceIdentifier,
                AdditionalData = additionalData
            };
        });

        _logger.LogInformation("Async correlation demo completed");
        return Ok(result);
    }

    /// <summary>
    /// Demonstrates nested method calls with correlation ID tracking
    /// </summary>
    [HttpGet("nested")]
    public async Task<ActionResult<CorrelationDemo>> GetNested()
    {
        _logger.LogInformation("Nested correlation demo started");
        
        var result = await CorrelationIdHelper.ExecuteWithCorrelationIdAsync(_correlationIdService, async () =>
        {
            _logger.LogDebug("Level 1: Starting nested operations");
            
            var level2Result = await ProcessLevel2Async();
            var level3Result = ProcessLevel3();
            
            _logger.LogDebug("All nested levels completed");
            
            return new CorrelationDemo
            {
                Message = "Nested correlation ID demo",
                CorrelationId = _correlationIdService.CorrelationId,
                Timestamp = DateTime.UtcNow,
                RequestId = HttpContext.TraceIdentifier,
                AdditionalData = $"Level2: {level2Result}, Level3: {level3Result}"
            };
        });

        _logger.LogInformation("Nested correlation demo completed");
        return Ok(result);
    }

    /// <summary>
    /// Shows correlation ID in error scenarios
    /// </summary>
    [HttpGet("error")]
    public ActionResult<CorrelationDemo> GetError()
    {
        _logger.LogInformation("Error correlation demo started");
        
        try
        {
            CorrelationIdHelper.ExecuteWithCorrelationId(_correlationIdService, () =>
            {
                _logger.LogDebug("About to simulate an error");
                throw new InvalidOperationException("Simulated error for correlation demo");
            });
            
            // This won't be reached due to the exception above
            return Ok(new CorrelationDemo
            {
                Message = "This should not be reached",
                CorrelationId = _correlationIdService.CorrelationId,
                Timestamp = DateTime.UtcNow,
                RequestId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in correlation demo");
            
            return StatusCode(500, new CorrelationDemo
            {
                Message = "Error occurred - check logs with correlation ID",
                CorrelationId = _correlationIdService.CorrelationId,
                Timestamp = DateTime.UtcNow,
                RequestId = HttpContext.TraceIdentifier,
                AdditionalData = ex.Message
            });
        }
    }

    /// <summary>
    /// Returns current correlation information
    /// </summary>
    [HttpGet("info")]
    public ActionResult<CorrelationInfo> GetCorrelationInfo()
    {
        _logger.LogInformation("Correlation info requested");
        
        var correlationId = _correlationIdService.CorrelationId;
        _logger.LogDebug("Current correlation ID: {CorrelationId}", correlationId);
        
        return Ok(new CorrelationInfo
        {
            CorrelationId = correlationId,
            RequestId = HttpContext.TraceIdentifier,
            RequestHeaders = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            Timestamp = DateTime.UtcNow,
            RequestPath = Request.Path,
            QueryString = Request.QueryString.ToString()
        });
    }

    // Private helper methods demonstrating nested correlation tracking

    private async Task<string> ProcessAdditionalDataAsync()
    {
        return await CorrelationIdHelper.ExecuteWithCorrelationIdAsync(_correlationIdService, async () =>
        {
            _logger.LogDebug("Processing additional data asynchronously");
            await Task.Delay(50);
            return $"AsyncData-{DateTime.UtcNow.Ticks % 1000}";
        });
    }

    private async Task<string> ProcessLevel2Async()
    {
        return await CorrelationIdHelper.ExecuteWithCorrelationIdAsync(_correlationIdService, async () =>
        {
            _logger.LogDebug("Level 2: Processing async operation");
            await Task.Delay(25);
            
            var level3Data = ProcessLevel3();
            return $"Level2-{level3Data}";
        });
    }

    private string ProcessLevel3()
    {
        return CorrelationIdHelper.ExecuteWithCorrelationId(_correlationIdService, () =>
        {
            _logger.LogDebug("Level 3: Processing synchronous operation");
            return $"Level3-{DateTime.UtcNow.Millisecond}";
        });
    }
}

public class CorrelationDemo
{
    public required string Message { get; set; }
    public required string CorrelationId { get; set; }
    public DateTime Timestamp { get; set; }
    public required string RequestId { get; set; }
    public string? AdditionalData { get; set; }
}

public class CorrelationInfo
{
    public required string CorrelationId { get; set; }
    public required string RequestId { get; set; }
    public required Dictionary<string, string> RequestHeaders { get; set; }
    public DateTime Timestamp { get; set; }
    public required string RequestPath { get; set; }
    public required string QueryString { get; set; }
}
