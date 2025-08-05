using System.Diagnostics;
using AspireApp1.CorrelationId;
using Microsoft.AspNetCore.Mvc;

namespace AspireApp1.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly ICorrelationIdService _correlationIdService;

    public HealthController(ILogger<HealthController> logger, ICorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }
    [HttpGet]
    public ActionResult<HealthStatus> Get()
    {
        _logger.LogInformation("Health check requested");
        
        return CorrelationIdHelper.ExecuteWithCorrelationId(_correlationIdService, () =>
        {
            _logger.LogDebug("Performing basic health check");
            
            var healthStatus = new HealthStatus
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
            };
            
            _logger.LogDebug("Health check completed successfully");
            return (ActionResult<HealthStatus>)Ok(healthStatus);
        });
    }

    [HttpGet("detailed")]
    public ActionResult<DetailedHealthStatus> GetDetailed()
    {
        var memoryUsage = GC.GetTotalMemory(false);
        var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();

        return Ok(new DetailedHealthStatus
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            MemoryUsageBytes = memoryUsage,
            UptimeSeconds = (long)uptime.TotalSeconds,
            MachineName = Environment.MachineName,
            ProcessorCount = Environment.ProcessorCount
        });
    }

    [HttpGet("ping")]
    public ActionResult<string> Ping()
    {
        return Ok("Pong");
    }
}

public class HealthStatus
{
    public required string Status { get; set; }
    public DateTime Timestamp { get; set; }
    public required string Version { get; set; }
    public required string Environment { get; set; }
}

public class DetailedHealthStatus : HealthStatus
{
    public long MemoryUsageBytes { get; set; }
    public long UptimeSeconds { get; set; }
    public required string MachineName { get; set; }
    public int ProcessorCount { get; set; }
}
