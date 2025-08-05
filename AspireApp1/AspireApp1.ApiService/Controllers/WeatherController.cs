using AspireApp1.ApiService.Services;
using AspireApp1.CorrelationId;
using Microsoft.AspNetCore.Mvc;

namespace AspireApp1.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherController> _logger;
    private readonly ICorrelationIdService _correlationIdService;
    private readonly IWeatherService _weatherService;

    public WeatherController(
        ILogger<WeatherController> logger, 
        ICorrelationIdService correlationIdService,
        IWeatherService weatherService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
        _weatherService = weatherService;
    }

    [HttpGet]
    public IEnumerable<WeatherForecastDto> Get()
    {
        // Correlation ID automatically available - no wrapper needed!
        _logger.LogInformation("Getting weather forecast");
        
        _logger.LogDebug("Generating weather forecast data");
        return Enumerable.Range(1, 5).Select(index => new WeatherForecastDto
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            Summaries[Random.Shared.Next(Summaries.Length)]
        ))
        .ToArray();
    }

    // Alternative endpoint that matches the original minimal API route
    [HttpGet]
    [Route("/weatherforecast")]
    public IEnumerable<WeatherForecastDto> GetWeatherForecast()
    {
        // Correlation ID automatically available - no wrapper needed!
        _logger.LogInformation("Getting weather forecast from legacy endpoint");
        
        _logger.LogDebug("Processing legacy weather forecast request");
        return Enumerable.Range(1, 5).Select(index => new WeatherForecastDto
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            Summaries[Random.Shared.Next(Summaries.Length)]
        ))
        .ToArray();
    }

    /// <summary>
    /// Demonstrates correlation ID flowing automatically to service methods
    /// </summary>
    [HttpGet("service/{city}")]
    public async Task<ActionResult<WeatherForecast>> GetWeatherUsingService(string city)
    {
        // Correlation ID automatically available in controller
        var correlationId = _correlationIdService.CorrelationId;
        
        _logger.LogInformation("Weather service request started for {City}", city);
        
        // Call service method - correlation ID flows automatically!
        var forecast = await _weatherService.GetWeatherAsync(city);
        
        _logger.LogInformation("Weather service request completed for {City}", city);
        
        // Correlation ID will be the same throughout the entire call chain
        return Ok(forecast);
    }

    /// <summary>
    /// Demonstrates parallel async operations with automatic correlation flow
    /// </summary>
    [HttpPost("multiple")]
    public async Task<ActionResult<List<WeatherForecast>>> GetMultipleCities([FromBody] List<string> cities)
    {
        var correlationId = _correlationIdService.CorrelationId;
        
        _logger.LogInformation("Multiple cities weather request started for {CityCount} cities", cities.Count);
        
        // Service handles parallel operations - correlation ID flows to all!
        var forecasts = await _weatherService.GetMultipleCitiesAsync(cities);
        
        _logger.LogInformation("Multiple cities weather completed");
        
        // All forecasts will have the same correlation ID
        return Ok(new
        {
            RequestCorrelationId = correlationId,
            CityCount = cities.Count,
            Forecasts = forecasts,
            AllSameCorrelationId = forecasts.All(f => f.CorrelationId == correlationId)
        });
    }

    [HttpGet("{days:int}")]
    public IEnumerable<WeatherForecast> Get(int days)
    {
        if (days < 1 || days > 30)
        {
            days = 5;
        }

        return Enumerable.Range(1, days).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }

    [HttpGet("city/{city}")]
    public ActionResult<WeatherForecast> GetByCity(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return BadRequest("City name is required");
        }

        var weather = new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)],
            City = city
        };

        return Ok(weather);
    }
}

// Record type matching the original minimal API structure
public record WeatherForecastDto(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class WeatherForecast
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
    public string? City { get; set; }
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
