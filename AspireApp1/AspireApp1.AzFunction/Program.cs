using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AspireApp1.CorrelationId.AzureFunctions;

var builder = FunctionsApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Add basic Correlation ID services with HTTP client integration
builder.Services.AddCorrelationIdWithHttpClient(options =>
{
    options.AdditionalHeaders.AddRange(new[]
    {          // Custom event tracking header
        "X-User-Id"          // Source system identifier
    });
    // Ensure auto-generation is enabled and logging is active
    options.AutoGenerate = true;
    options.LogFunctionExecution = true;
    options.AddToResponseHeaders = true;
});

// Add HTTP clients for external API calls
builder.Services.AddHttpClient(CorrelationIdHttpClientNames.ExternalApi, client =>
{
    client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient(CorrelationIdHttpClientNames.InternalService, client =>
{
    client.BaseAddress = new Uri("https://httpbin.org/");
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Build().Run();
