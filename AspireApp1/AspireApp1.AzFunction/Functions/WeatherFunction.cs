using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using AspireApp1.CorrelationId.AzureFunctions;

namespace AspireApp1.AzFunction.Functions;

/// <summary>
/// Weather Function demonstrating correlation ID and additional headers functionality
/// </summary>
public class WeatherFunction : CorrelatedHttpFunction
{
    private readonly ICorrelatedHttpClient _httpClient;

    public WeatherFunction(
        ILoggerFactory loggerFactory, 
        IEnhancedCorrelationIdService correlationIdService,
        IOptions<CorrelationIdOptions> options,
        ICorrelatedHttpClient httpClient)
        : base(loggerFactory.CreateLogger<WeatherFunction>(), correlationIdService, options)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Get weather forecast with automatic correlation ID tracking
    /// Try calling with headers: X-Custom-Correlation-Id, X-User-Id, X-Event-Id
    /// </summary>
    [Function("GetWeather")]
    public async Task<HttpResponseData> GetWeather(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "weather")] HttpRequestData req)
    {
        return await ExecuteWithCorrelationAsync(req, async () =>
        {
            Logger.LogInformation("Processing weather request");

            // Simulate some processing
            await Task.Delay(100);

            var forecast = new
            {
                Date = DateTime.Now,
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = GetRandomSummary(),
                Location = "Sample City",
                CorrelationId = CorrelationIdService.CorrelationId,
                CapturedHeaders = CorrelationIdService.CapturedHeaders,
                EventId = CorrelationIdService.GetHeader("X-Event-Id"),
                UserId = CorrelationIdService.GetHeader("X-User-Id"),
                RequestSource = CorrelationIdService.GetHeader("X-Request-Source")
            };

            Logger.LogInformation("Weather forecast generated for {Location}", forecast.Location);

            return await CreateJsonResponseAsync(req, forecast);
        });
    }

    /// <summary>
    /// Get weather with external API call to demonstrate HTTP client propagation
    /// </summary>
    [Function("GetWeatherWithExternalCall")]
    public async Task<HttpResponseData> GetWeatherWithExternalCall(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "weather/external")] HttpRequestData req)
    {
        return await ExecuteWithCorrelationAsync(req, async () =>
        {
            Logger.LogInformation("Processing weather request with external API call");

            // Call external API - correlation headers are automatically propagated
            Logger.LogInformation("Calling external API for additional data");
            var externalData = await _httpClient.GetAsync("posts/1");
            
            Logger.LogInformation("External API call completed");

            var forecast = new
            {
                Date = DateTime.Now,
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = GetRandomSummary(),
                Location = "Sample City",
                ExternalApiResponse = await externalData.Content.ReadAsStringAsync(),
                CorrelationId = CorrelationIdService.CorrelationId,
                CapturedHeaders = CorrelationIdService.CapturedHeaders
            };

            Logger.LogInformation("Weather forecast with external data generated");

            return await CreateJsonResponseAsync(req, forecast);
        });
    }

    /// <summary>
    /// Update weather data with additional headers set programmatically
    /// </summary>
    [Function("UpdateWeather")]
    public async Task<HttpResponseData> UpdateWeather(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "weather")] HttpRequestData req)
    {
        return await ExecuteWithCorrelationAsync(req, async () =>
        {
            Logger.LogInformation("Processing weather update request");

            // Read request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            
            // Set additional headers programmatically
            CorrelationIdService.SetAdditionalHeaders(new Dictionary<string, string>
            {
                { "X-Processing-Stage", "weather-update" },
                { "X-Request-Priority", "high" },
                { "X-Operation-Type", "update" }
            });

            Logger.LogInformation("Additional headers set programmatically");

            // Simulate processing
            await Task.Delay(200);

            var result = new
            {
                Message = "Weather data updated successfully",
                Timestamp = DateTime.UtcNow,
                RequestData = requestBody,
                CorrelationId = CorrelationIdService.CorrelationId,
                AllHeaders = CorrelationIdService.CapturedHeaders
            };

            Logger.LogInformation("Weather update completed");

            return await CreateJsonResponseAsync(req, result);
        });
    }

    private static string GetRandomSummary()
    {
        string[] summaries = [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild",
            "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];
        return summaries[Random.Shared.Next(summaries.Length)];
    }
}
