using AspireApp1.CorrelationId;
using Microsoft.AspNetCore.Mvc;

namespace AspireApp1.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StructuredLoggingDemoController : ControllerBase
{
    private readonly ILogger<StructuredLoggingDemoController> _logger;
    private readonly ICorrelationIdService _correlationIdService;

    public StructuredLoggingDemoController(ILogger<StructuredLoggingDemoController> logger, ICorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    /// <summary>
    /// Demonstrates automatic structured logging with correlation ID (no extra setup needed)
    /// </summary>
    [HttpGet("auto-structured")]
    public ActionResult<LoggingDemoResponse> GetAutoStructured()
    {
        var correlationId = _correlationIdService.CorrelationId;
        
        // All these logs automatically include CorrelationId as structured properties
        _logger.LogInformation("Starting automatic structured logging demonstration");
        _logger.LogDebug("Processing request - correlation ID automatically included");
        _logger.LogInformation("Business logic execution started");

        // Simulate some business logic
        var processingResult = ProcessBusinessLogic();
        
        _logger.LogInformation("Business logic completed successfully with Result: {ProcessingResult}", processingResult);

        var response = new LoggingDemoResponse
        {
            Message = "Automatic structured logging demonstration - CorrelationId included in all logs automatically",
            CorrelationId = correlationId,
            ProcessingResult = processingResult,
            Timestamp = DateTime.UtcNow,
            StructuredProperties = new Dictionary<string, object>
            {
                ["Note"] = "CorrelationId is automatically added to all log entries",
                ["AutomaticProperties"] = "CorrelationId",
                ["UserSetup"] = "None required"
            }
        };

        _logger.LogInformation("Response prepared - correlation tracking automatic");
        return Ok(response);
    }

    /// <summary>
    /// Shows that errors also automatically include correlation ID in structured format
    /// </summary>
    [HttpGet("auto-error")]
    public ActionResult<LoggingDemoResponse> GetAutoError()
    {
        var correlationId = _correlationIdService.CorrelationId;
        
        _logger.LogInformation("Starting error demonstration - correlation ID automatic");

        try
        {
            // Simulate an error
            throw new InvalidOperationException("This is a simulated error - correlation ID automatically tracked");
        }
        catch (Exception ex)
        {
            // This error log automatically includes CorrelationId as structured properties
            _logger.LogError(ex, "An error occurred - correlation ID automatically included in structured properties");

            return StatusCode(500, new LoggingDemoResponse
            {
                Message = "Error occurred - check logs with correlation ID (automatically included)",
                CorrelationId = correlationId,
                ProcessingResult = "ERROR",
                Timestamp = DateTime.UtcNow,
                StructuredProperties = new Dictionary<string, object>
                {
                    ["Note"] = "Error logs automatically include CorrelationId for tracking",
                    ["AutomaticTracking"] = "Yes",
                    ["ErrorMessage"] = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Shows correlation ID propagation across multiple method calls
    /// </summary>
    [HttpGet("propagation")]
    public async Task<ActionResult<LoggingDemoResponse>> GetPropagationDemo()
    {
        var correlationId = _correlationIdService.CorrelationId;
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Operation"] = "PropagationDemo",
            ["MethodLevel"] = "Controller"
        });

        _logger.LogInformation("Starting propagation demo at controller level");

        var result = await ProcessWithPropagationAsync();

        return Ok(new LoggingDemoResponse
        {
            Message = "Correlation ID propagation demo completed",
            CorrelationId = correlationId,
            ProcessingResult = result,
            Timestamp = DateTime.UtcNow,
            StructuredProperties = new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["Operation"] = "PropagationDemo",
                ["FinalResult"] = result
            }
        });
    }

    private string ProcessBusinessLogic()
    {
        var correlationId = _correlationIdService.CorrelationId;
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["MethodLevel"] = "BusinessLogic",
            ["ProcessingStep"] = "DataProcessing"
        });

        _logger.LogDebug("Processing business logic with CorrelationId: {CorrelationId}", correlationId);
        
        // Simulate processing
        Thread.Sleep(50);
        
        var result = $"PROCESSED_{DateTime.UtcNow.Ticks % 10000}";
        _logger.LogDebug("Business logic completed with result: {Result}", result);
        
        return result;
    }

    private async Task<string> ProcessWithPropagationAsync()
    {
        var correlationId = _correlationIdService.CorrelationId;
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["MethodLevel"] = "AsyncMethod",
            ["ProcessingStep"] = "AsyncProcessing"
        });

        _logger.LogInformation("Starting async processing with CorrelationId: {CorrelationId}", correlationId);
        
        await Task.Delay(100);
        
        var nestedResult = ProcessNestedOperation();
        
        _logger.LogInformation("Async processing completed with nested result: {NestedResult}", nestedResult);
        
        return $"ASYNC_{nestedResult}";
    }

    private string ProcessNestedOperation()
    {
        var correlationId = _correlationIdService.CorrelationId;
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["MethodLevel"] = "NestedOperation",
            ["ProcessingStep"] = "FinalStep"
        });

        _logger.LogDebug("Executing nested operation with CorrelationId: {CorrelationId}", correlationId);
        
        var result = $"NESTED_{DateTime.UtcNow.Millisecond}";
        _logger.LogDebug("Nested operation result: {Result}", result);
        
        return result;
    }
}

public class LoggingDemoResponse
{
    public required string Message { get; set; }
    public required string CorrelationId { get; set; }
    public required string ProcessingResult { get; set; }
    public DateTime Timestamp { get; set; }
    public required Dictionary<string, object> StructuredProperties { get; set; }
}
