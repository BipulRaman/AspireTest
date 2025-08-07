using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AspireApp1.CorrelationId.AzureFunctions.Examples;

/// <summary>
/// Example Azure Function demonstrating additional headers feature alongside correlation ID
/// </summary>
public class AdditionalHeadersFunction : CorrelatedHttpFunction
{
    public AdditionalHeadersFunction(ILoggerFactory loggerFactory, IEnhancedCorrelationIdService correlationIdService)
        : base(loggerFactory.CreateLogger<AdditionalHeadersFunction>(), correlationIdService)
    {
    }

    /// <summary>
    /// Demonstrates automatic capturing of additional headers
    /// Send requests with headers like X-Event-Id, X-User-Id, X-Request-Source
    /// </summary>
    [Function("AdditionalHeadersDemo")]
    public async Task<HttpResponseData> GetDemo(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "additional-headers/demo")] HttpRequestData req)
    {
        return await ExecuteWithCorrelationAsync(req, async () =>
        {
            Logger.LogInformation("Processing demo request with automatic header capture");

            var response = new
            {
                Message = "Additional headers demo for Azure Functions",
                Timestamp = DateTime.UtcNow,
                CapturedHeaders = CorrelationIdService.CapturedHeaders,
                CorrelationId = CorrelationIdService.CorrelationId,
                EventId = CorrelationIdService.GetHeader("X-Event-Id"),
                UserId = CorrelationIdService.GetHeader("X-User-Id"),
                RequestSource = CorrelationIdService.GetHeader("X-Request-Source")
            };

            Logger.LogInformation("Demo completed. Captured headers: {CapturedHeaders}",
                string.Join(", ", CorrelationIdService.CapturedHeaders.Select(h => $"{h.Key}={h.Value}")));

            return await CreateJsonResponseAsync(req, response);
        });
    }

    /// <summary>
    /// Demonstrates setting additional headers programmatically
    /// </summary>
    [Function("SetAdditionalHeaders")]
    public async Task<HttpResponseData> SetAdditionalHeaders(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "additional-headers/set")] HttpRequestData req)
    {
        return await ExecuteWithCorrelationAsync(req, async () =>
        {
            Logger.LogInformation("Setting additional headers programmatically");

            // Read headers from request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var headers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody) ?? new();

            // Set additional headers programmatically
            CorrelationIdService.SetAdditionalHeaders(headers);

            Logger.LogInformation("Additional headers set successfully");

            var response = new
            {
                Message = "Additional headers set programmatically in Azure Function",
                Timestamp = DateTime.UtcNow,
                AllCapturedHeaders = CorrelationIdService.CapturedHeaders,
                CorrelationId = CorrelationIdService.CorrelationId
            };

            return await CreateJsonResponseAsync(req, response);
        });
    }

    /// <summary>
    /// Demonstrates header flow through multiple log statements in Azure Functions
    /// </summary>
    [Function("FlowDemo")]
    public async Task<HttpResponseData> GetFlowDemo(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "additional-headers/flow/{eventType}")] HttpRequestData req,
        string eventType)
    {
        return await ExecuteWithCorrelationAsync(req, async () =>
        {
            Logger.LogInformation("Starting flow demo for event type: {EventType}", eventType);

            // Simulate some processing steps
            await ProcessStep1();
            await ProcessStep2();
            await ProcessStep3();

            Logger.LogInformation("Flow demo completed for event type: {EventType}", eventType);

            var response = new
            {
                Message = $"Flow demo completed for {eventType} in Azure Function",
                Timestamp = DateTime.UtcNow,
                CapturedHeaders = CorrelationIdService.CapturedHeaders,
                Steps = new[] { "Step1", "Step2", "Step3" }
            };

            return await CreateJsonResponseAsync(req, response);
        });
    }

    private async Task ProcessStep1()
    {
        Logger.LogInformation("Executing Step 1 - Data validation");
        await Task.Delay(50); // Simulate processing
        Logger.LogInformation("Step 1 completed successfully");
    }

    private async Task ProcessStep2()
    {
        Logger.LogInformation("Executing Step 2 - Business logic processing");
        await Task.Delay(100); // Simulate processing
        Logger.LogInformation("Step 2 completed successfully");
    }

    private async Task ProcessStep3()
    {
        Logger.LogInformation("Executing Step 3 - Response preparation");
        await Task.Delay(25); // Simulate processing
        Logger.LogInformation("Step 3 completed successfully");
    }
}
