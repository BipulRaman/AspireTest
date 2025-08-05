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

    public WeatherController(ILogger<WeatherController> logger, ICorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    [HttpGet]
    public IEnumerable<WeatherForecastDto> Get()
    {
        _logger.LogInformation("Getting weather forecast");
        
        return CorrelationIdHelper.ExecuteWithCorrelationId(_correlationIdService, () =>
        {
            _logger.LogDebug("Generating weather forecast data");
            return Enumerable.Range(1, 5).Select(index => new WeatherForecastDto
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                Summaries[Random.Shared.Next(Summaries.Length)]
            ))
            .ToArray();
        });
    }

    // Alternative endpoint that matches the original minimal API route
    [HttpGet]
    [Route("/weatherforecast")]
    public IEnumerable<WeatherForecastDto> GetWeatherForecast()
    {
        _logger.LogInformation("Getting weather forecast from legacy endpoint");
        
        return CorrelationIdHelper.ExecuteWithCorrelationId(_correlationIdService, () =>
        {
            _logger.LogDebug("Processing legacy weather forecast request");
            return Enumerable.Range(1, 5).Select(index => new WeatherForecastDto
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                Summaries[Random.Shared.Next(Summaries.Length)]
            ))
            .ToArray();
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
