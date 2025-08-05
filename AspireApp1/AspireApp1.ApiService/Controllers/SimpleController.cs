using Microsoft.AspNetCore.Mvc;

namespace AspireApp1.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimpleController : ControllerBase
{
    private readonly ILogger<SimpleController> _logger;
    // ❌ Notice: NO ICorrelationIdService injection!

    public SimpleController(ILogger<SimpleController> logger)
    {
        _logger = logger;
        // Only logger injection - no correlation service
    }

    [HttpGet("basic")]
    public ActionResult<BasicResponse> GetBasic()
    {
        // ✅ This log automatically includes correlation ID via middleware
        _logger.LogInformation("Processing basic request - correlation ID flows automatically");

        _logger.LogDebug("Performing basic operation");
        
        // Simulate some work
        Thread.Sleep(50);
        
        _logger.LogDebug("Basic operation processing completed");

        var result = new BasicResponse
        {
            Message = "Basic operation completed successfully",
            Timestamp = DateTime.UtcNow,
            Status = "Success"
            // ❌ Can't include CorrelationId here without injection
        };

        _logger.LogInformation("Basic request completed - check logs for automatic correlation ID");
        return Ok(result);
    }

    [HttpGet("async-operation")]
    public async Task<ActionResult<BasicResponse>> GetAsyncOperation()
    {
        // ✅ Correlation ID automatically flows through async operations
        _logger.LogInformation("Starting async operation without correlation DI");

        _logger.LogDebug("Processing async request - correlation flows automatically");
        
        // ✅ Async operations maintain correlation context automatically
        await SimulateAsyncWork();
        await ProcessDataAsync();
        
        _logger.LogDebug("Async operation completed");

        var result = new BasicResponse
        {
            Message = "Async operation completed - correlation tracked in logs",
            Timestamp = DateTime.UtcNow,
            Status = "Completed"
        };

        _logger.LogInformation("Async operation finished - correlation ID in all log entries");
        return Ok(result);
    }

    [HttpGet("nested-calls")]
    public async Task<ActionResult<BasicResponse>> GetNestedCalls()
    {
        // ✅ Correlation flows through all nested calls automatically
        _logger.LogInformation("Starting nested calls - no correlation DI needed");

        await Level1OperationAsync();

        var result = new BasicResponse
        {
            Message = "Nested calls completed - correlation in all nested logs",
            Timestamp = DateTime.UtcNow,
            Status = "AllLevelsCompleted"
        };

        _logger.LogInformation("All nested operations completed");
        return Ok(result);
    }

    [HttpGet("parallel-work")]
    public async Task<ActionResult<BasicResponse>> GetParallelWork()
    {
        _logger.LogInformation("Starting parallel work - correlation flows to all tasks");

        // ✅ Even parallel operations maintain correlation automatically
        var tasks = new[]
        {
            ParallelTask1Async(),
            ParallelTask2Async(),
            ParallelTask3Async()
        };

        await Task.WhenAll(tasks);

        _logger.LogInformation("All parallel tasks completed - each task logged with correlation");

        return Ok(new BasicResponse
        {
            Message = "Parallel work completed - correlation in all parallel logs",
            Timestamp = DateTime.UtcNow,
            Status = "ParallelCompleted"
        });
    }

    [HttpPost("process-data")]
    public async Task<ActionResult<BasicResponse>> ProcessData([FromBody] ProcessRequest request)
    {
        // ✅ Logging includes correlation ID automatically
        _logger.LogInformation("Processing data for request: {RequestType}", request.RequestType);

        _logger.LogDebug("Validating request data");
        await ValidateRequestAsync(request);

        _logger.LogDebug("Processing request data");
        await ProcessRequestDataAsync(request);

        _logger.LogDebug("Finalizing request processing");
        await FinalizeProcessingAsync();

        _logger.LogInformation("Data processing completed successfully");

        return Ok(new BasicResponse
        {
            Message = $"Data processing completed for {request.RequestType}",
            Timestamp = DateTime.UtcNow,
            Status = "ProcessingCompleted"
        });
    }

    // ✅ All private methods automatically maintain correlation context
    private async Task SimulateAsyncWork()
    {
        _logger.LogDebug("Simulating async work - correlation automatic");
        await Task.Delay(100);
        _logger.LogDebug("Async work simulation completed");
    }

    private async Task ProcessDataAsync()
    {
        _logger.LogDebug("Processing data asynchronously - correlation flows");
        await Task.Delay(75);
        _logger.LogDebug("Data processing finished");
    }

    private async Task Level1OperationAsync()
    {
        _logger.LogDebug("Level 1 operation started - correlation flows automatically");
        await Level2OperationAsync();
        _logger.LogDebug("Level 1 operation completed");
    }

    private async Task Level2OperationAsync()
    {
        _logger.LogDebug("Level 2 operation started - still has correlation");
        await Level3OperationAsync();
        _logger.LogDebug("Level 2 operation completed");
    }

    private async Task Level3OperationAsync()
    {
        _logger.LogDebug("Level 3 operation (deepest) - correlation still flows");
        await Task.Delay(25);
        _logger.LogDebug("Level 3 operation completed");
    }

    private async Task ParallelTask1Async()
    {
        _logger.LogDebug("Parallel Task 1 started - correlation maintained");
        await Task.Delay(80);
        _logger.LogDebug("Parallel Task 1 completed");
    }

    private async Task ParallelTask2Async()
    {
        _logger.LogDebug("Parallel Task 2 started - correlation maintained");
        await Task.Delay(120);
        _logger.LogDebug("Parallel Task 2 completed");
    }

    private async Task ParallelTask3Async()
    {
        _logger.LogDebug("Parallel Task 3 started - correlation maintained");
        await Task.Delay(60);
        _logger.LogDebug("Parallel Task 3 completed");
    }

    private async Task ValidateRequestAsync(ProcessRequest request)
    {
        _logger.LogDebug("Validating request: {RequestType} - correlation flows", request.RequestType);
        await Task.Delay(30);
        _logger.LogDebug("Request validation completed");
    }

    private async Task ProcessRequestDataAsync(ProcessRequest request)
    {
        _logger.LogDebug("Processing request data: {RequestType} - correlation automatic", request.RequestType);
        await Task.Delay(150);
        _logger.LogDebug("Request data processing completed");
    }

    private async Task FinalizeProcessingAsync()
    {
        _logger.LogDebug("Finalizing processing - correlation still available");
        await Task.Delay(40);
        _logger.LogDebug("Processing finalization completed");
    }
}

public class BasicResponse
{
    public required string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public required string Status { get; set; }
    // ❌ Notice: No CorrelationId property because we can't access it without DI
}

public class ProcessRequest
{
    public required string RequestType { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}
