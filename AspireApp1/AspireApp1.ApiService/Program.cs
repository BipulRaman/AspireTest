using AspireApp1.CorrelationId;
using AspireApp1.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddControllers();

// Add Correlation ID services with HTTP client integration and additional headers
builder.Services.AddCorrelationIdWithHttpClient(options =>
{
    // Configure additional headers to capture and log alongside correlation ID
    options.AdditionalHeaders.AddRange(new[]
    {          // Custom event tracking header
        "X-User-Id"          // Source system identifier
    });
    
    // Add captured headers to response for client tracking
    options.AddAdditionalHeadersToResponse = true;
});

// Add our weather service
builder.Services.AddScoped<IWeatherService, WeatherService>();

// Add API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// Add Correlation ID middleware (should be early in the pipeline)
app.UseCorrelationId();

// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirect root to Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
