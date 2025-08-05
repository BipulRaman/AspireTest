using AspireApp1.CorrelationId;
using Microsoft.AspNetCore.Mvc;

namespace AspireApp1.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SampleController : ControllerBase
{
    private readonly ILogger<SampleController> _logger;
    private readonly ICorrelationIdService _correlationIdService;

    public SampleController(ILogger<SampleController> logger, ICorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    [HttpGet("sync")]
    public ActionResult<SampleResponse> GetSync()
    {
        _logger.LogInformation("Starting synchronous operation");

        _logger.LogDebug("Processing synchronous request");
        
        // Simulate some work
        Thread.Sleep(100);
        
        var result = new SampleResponse
        {
            Message = "Synchronous operation completed",
            Timestamp = DateTime.UtcNow,
            CorrelationId = _correlationIdService.CorrelationId
        };

        _logger.LogInformation("Synchronous operation completed successfully");
        return Ok(result);
    }

    [HttpGet("async")]
    public async Task<ActionResult<SampleResponse>> GetAsync()
    {
        _logger.LogInformation("Starting asynchronous operation");

        _logger.LogDebug("Processing asynchronous request");
        
        // Simulate some async work
        await Task.Delay(100);
        
        var result = new SampleResponse
        {
            Message = "Asynchronous operation completed",
            Timestamp = DateTime.UtcNow,
            CorrelationId = _correlationIdService.CorrelationId
        };

        _logger.LogInformation("Asynchronous operation completed successfully");
        return Ok(result);
    }

    [HttpGet("nested")]
    public async Task<ActionResult<SampleResponse>> GetNested()
    {
        _logger.LogInformation("Starting nested operation");

        _logger.LogDebug("Starting first level of nested operation");
        
        var intermediateResult = await ProcessNestedOperation();
        
        _logger.LogDebug("Completed nested operations");
        
        var result = new SampleResponse
        {
            Message = $"Nested operation completed: {intermediateResult}",
            Timestamp = DateTime.UtcNow,
            CorrelationId = _correlationIdService.CorrelationId
        };

        _logger.LogInformation("Nested operation completed successfully");
        return Ok(result);
    }

    private async Task<string> ProcessNestedOperation()
    {
        _logger.LogDebug("Processing nested operation level 2");
        await Task.Delay(50);
        
        var deepResult = ProcessDeepOperation();
        _logger.LogDebug("Deep operation result: {Result}", deepResult);
        
        return $"Nested-{deepResult}";
    }

    private string ProcessDeepOperation()
    {
        _logger.LogDebug("Processing deep operation level 3");
        return $"Deep-{DateTime.UtcNow.Ticks % 1000}";
    }

    [HttpGet("correlation-info")]
    public ActionResult<CorrelationInfo> GetCorrelationInfo()
    {
        _logger.LogInformation("Retrieving correlation information");
        
        return Ok(new CorrelationInfo
        {
            CorrelationId = _correlationIdService.CorrelationId,
            RequestId = HttpContext.TraceIdentifier,
            RequestHeaders = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            Timestamp = DateTime.UtcNow,
            RequestPath = Request.Path,
            QueryString = Request.QueryString.ToString()
        });
    }
}

public class SampleResponse
{
    public required string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public required string CorrelationId { get; set; }
}
