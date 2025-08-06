using AspireApp1.Web;
using AspireApp1.Web.Components;
using AspireApp1.CorrelationId;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add Correlation ID services with configuration for additional headers
builder.Services.AddCorrelationId(options =>
{
    // Configure additional headers to capture for web requests
    options.AdditionalHeaders.AddRange(new[]
    {
        "X-Event-Id",           // Custom event tracking
        "X-User-Session-Id",    // User session tracking
        "X-Browser-Id"          // Browser fingerprint tracking
    });
    
    // Add captured headers to response for client-side tracking
    options.AddAdditionalHeadersToResponse = true;
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

builder.Services.AddHttpClient<WeatherApiClient>(client =>
    {
        // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
        // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
        client.BaseAddress = new("https+http://apiservice");
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Add correlation ID middleware early in the pipeline
app.UseCorrelationId();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseOutputCache();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
