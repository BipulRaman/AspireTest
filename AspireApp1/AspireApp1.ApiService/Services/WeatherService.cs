using AspireApp1.CorrelationId;

namespace AspireApp1.ApiService.Services;

public interface IWeatherService
{
    Task<WeatherForecast> GetWeatherAsync(string city);
    WeatherForecast GetWeatherSync(string city);
    Task<List<WeatherForecast>> GetMultipleCitiesAsync(List<string> cities);
}

public class WeatherService : IWeatherService
{
    private readonly ILogger<WeatherService> _logger;
    private readonly ICorrelationIdService _correlationIdService;
    private readonly HttpClient _httpClient;

    public WeatherService(ILogger<WeatherService> logger, ICorrelationIdService correlationIdService, HttpClient httpClient)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
        _httpClient = httpClient;
    }

    public async Task<WeatherForecast> GetWeatherAsync(string city)
    {
        // Correlation ID automatically available in service methods!
        var correlationId = _correlationIdService.CorrelationId;
        
        _logger.LogInformation("Getting weather for city: {City}", city);
        
        // Call nested service methods - correlation ID flows automatically
        var temperature = await GetTemperatureAsync(city);
        var conditions = await GetConditionsAsync(city);
        
        // Make HTTP calls - correlation ID header automatically added
        // await _httpClient.GetAsync($"https://external-weather-api.com/weather/{city}");
        
        var forecast = new WeatherForecast
        {
            City = city,
            Temperature = temperature,
            Conditions = conditions,
            CorrelationId = correlationId, // Same correlation ID throughout
            Timestamp = DateTime.UtcNow
        };
        
        _logger.LogInformation("Weather forecast completed for {City}", city);
        return forecast;
    }

    public WeatherForecast GetWeatherSync(string city)
    {
        // Correlation ID automatically available in sync methods too!
        var correlationId = _correlationIdService.CorrelationId;
        
        _logger.LogInformation("Getting sync weather for city: {City}", city);
        
        // Call nested sync methods - correlation ID available
        var temperature = GetTemperatureSync(city);
        var conditions = GetConditionsSync(city);
        
        _logger.LogInformation("Sync weather forecast completed for {City}", city);
        
        return new WeatherForecast
        {
            City = city,
            Temperature = temperature,
            Conditions = conditions,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<List<WeatherForecast>> GetMultipleCitiesAsync(List<string> cities)
    {
        var correlationId = _correlationIdService.CorrelationId;
        
        _logger.LogInformation("Getting weather for {CityCount} cities", cities.Count);
        
        // Parallel async operations - correlation ID flows to all of them!
        var tasks = cities.Select(async city =>
        {
            _logger.LogDebug("Processing city: {City}", city);
            return await GetWeatherAsync(city); // Correlation ID flows automatically
        });
        
        var results = await Task.WhenAll(tasks);
        
        _logger.LogInformation("Completed weather for all {CityCount} cities", cities.Count);
        return results.ToList();
    }

    // Private helper methods - correlation ID automatically available
    private async Task<int> GetTemperatureAsync(string city)
    {
        _logger.LogDebug("Getting temperature for {City}", city);
        
        // Simulate async work
        await Task.Delay(Random.Shared.Next(50, 200));
        
        // Correlation ID available here automatically
        var correlationId = _correlationIdService.CorrelationId;
        _logger.LogDebug("Temperature lookup completed for {City} with correlation {CorrelationId}", 
            city, correlationId);
        
        return Random.Shared.Next(-10, 40);
    }

    private async Task<string> GetConditionsAsync(string city)
    {
        _logger.LogDebug("Getting conditions for {City}", city);
        
        await Task.Delay(Random.Shared.Next(30, 100));
        
        var conditions = new[] { "Sunny", "Cloudy", "Rainy", "Snowy" };
        return conditions[Random.Shared.Next(conditions.Length)];
    }

    private int GetTemperatureSync(string city)
    {
        _logger.LogDebug("Getting sync temperature for {City}", city);
        
        // Even in sync methods, correlation ID is available
        var correlationId = _correlationIdService.CorrelationId;
        _logger.LogDebug("Sync temperature lookup for {City} with correlation {CorrelationId}", 
            city, correlationId);
        
        Thread.Sleep(Random.Shared.Next(50, 150));
        return Random.Shared.Next(-10, 40);
    }

    private string GetConditionsSync(string city)
    {
        _logger.LogDebug("Getting sync conditions for {City}", city);
        
        Thread.Sleep(Random.Shared.Next(30, 80));
        
        var conditions = new[] { "Sunny", "Cloudy", "Rainy", "Snowy" };
        return conditions[Random.Shared.Next(conditions.Length)];
    }
}

public class WeatherForecast
{
    public required string City { get; set; }
    public int Temperature { get; set; }
    public required string Conditions { get; set; }
    public required string CorrelationId { get; set; }
    public DateTime Timestamp { get; set; }
}
