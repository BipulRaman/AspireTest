# AspireApp1.CorrelationId

A simple and basic implementation of correlation ID tracking for ASP.NET Core applications with **HTTP client integration** for distributed tracing.

## Features

- **Automatic Header Tracking**: Tracks `X-Correlation-Id` header on all incoming requests
- **Auto-Generation**: Generates new correlation ID if header is missing
- **Automatic Logging**: Adds correlation ID to all log entries (both message prefix and structured properties)
- **Structured Logging**: Adds correlation ID as custom properties for searchable metadata
- **Sync/Async Support**: Provides helpers for both synchronous and asynchronous method execution
- **Thread-Safe**: Uses `AsyncLocal<T>` for thread-safe correlation ID storage
- **HTTP Client Integration**: Automatically propagates correlation ID to outgoing HTTP requests
- **Named HTTP Clients**: Support for multiple configured HTTP clients with correlation
- **Distributed Tracing**: End-to-end correlation across microservices

## Usage

### 1. Basic Setup (Correlation ID only)

```csharp
builder.Services.AddCorrelationId();
app.UseCorrelationId();
```

### 2. Advanced Setup (With HTTP Client Integration)

```csharp
// Add correlation ID with HTTP client support
builder.Services.AddCorrelationIdWithHttpClient();

// Configure middleware
app.UseCorrelationId();
```

### 3. Custom HTTP Client Configuration

```csharp
// Add specific HTTP clients with correlation ID support
builder.Services.AddHttpClient("MyApiClient", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddCorrelationId();

// Or use named clients
builder.Services.AddHttpClient(CorrelationIdHttpClientNames.ExternalApi, client =>
{
    client.BaseAddress = new Uri("https://external-api.example.com");
}).AddCorrelationId();
```

## Usage Examples

### 1. Basic Controller Usage

```csharp
[ApiController]
public class MyController : ControllerBase
{
    private readonly ILogger<MyController> _logger;
    private readonly ICorrelationIdService _correlationIdService;

    public MyController(ILogger<MyController> logger, ICorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        // All logs automatically include correlation ID in both message and structured properties
        _logger.LogInformation("Processing request"); // Automatically includes CorrelationId
        
        // For sync methods with automatic correlation tracking
        var result = CorrelationIdHelper.ExecuteWithCorrelationId(_correlationIdService, () =>
        {
            _logger.LogDebug("Inside sync operation"); // Also automatically includes correlation ID
            return "Result";
        });

        return Ok(result);
    }
}
```

### 2. HTTP Client Integration (Recommended)

```csharp
[ApiController]
public class ExternalApiController : ControllerBase
{
    private readonly ICorrelatedHttpClient _httpClient;
    private readonly ILogger<ExternalApiController> _logger;

    public ExternalApiController(ICorrelatedHttpClient httpClient, ILogger<ExternalApiController> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    [HttpGet("external-data")]
    public async Task<IActionResult> GetExternalData()
    {
        _logger.LogInformation("Calling external API");

        // Correlation ID is automatically added to the outgoing request
        var data = await _httpClient.GetAsync<ExternalData>("https://api.example.com/data");

        _logger.LogInformation("External API call completed");
        return Ok(data);
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessData([FromBody] ProcessRequest request)
    {
        _logger.LogInformation("Starting data processing");

        // Multiple HTTP calls with automatic correlation ID propagation
        var tasks = new[]
        {
            _httpClient.GetAsync<UserInfo>($"https://user-service.com/users/{request.UserId}"),
            _httpClient.PostAsJsonAsync<OrderRequest, OrderResponse>(
                "https://order-service.com/orders", 
                new OrderRequest { UserId = request.UserId, Items = request.Items })
        };

        var results = await Task.WhenAll(tasks);

        _logger.LogInformation("Data processing completed");
        return Ok(new { User = results[0], Order = results[1] });
    }
}
```

### 3. Named HTTP Client Usage

```csharp
[ApiController]
public class IntegrationController : ControllerBase
{
    private readonly HttpClient _externalApiClient;
    private readonly HttpClient _internalServiceClient;
    private readonly ILogger<IntegrationController> _logger;

    public IntegrationController(IHttpClientFactory httpClientFactory, ILogger<IntegrationController> logger)
    {
        _externalApiClient = httpClientFactory.CreateClient(CorrelationIdHttpClientNames.ExternalApi);
        _internalServiceClient = httpClientFactory.CreateClient(CorrelationIdHttpClientNames.InternalService);
        _logger = logger;
    }

    [HttpGet("integration")]
    public async Task<IActionResult> IntegrationCall()
    {
        _logger.LogInformation("Starting integration calls");

        // Both clients automatically include correlation ID headers
        var externalTask = _externalApiClient.GetAsync("https://external-api.com/status");
        var internalTask = _internalServiceClient.GetAsync("https://internal-service.com/health");

        var responses = await Task.WhenAll(externalTask, internalTask);

        _logger.LogInformation("Integration calls completed");
        return Ok(new { 
            ExternalStatus = (int)responses[0].StatusCode,
            InternalStatus = (int)responses[1].StatusCode
        });
    }
}
```

## Automatic Structured Logging

The implementation automatically adds correlation ID as structured properties to **every log entry** with no additional setup required:

### Automatic Properties (Added to Every Log)
- `CorrelationId`: The correlation ID value

### How It Works Automatically
```csharp
// Just log normally - correlation ID properties are added automatically
_logger.LogInformation("Processing order {OrderId}", orderId);

// Results in log entry with:
// Message: "[CorrelationId: 12345678-1234-1234-1234-123456789abc] Processing order 12345"
// Structured Properties: 
//   - CorrelationId: "12345678-1234-1234-1234-123456789abc"
//   - OrderId: 12345
```

### Benefits for Log Analysis
- **Automatic**: No manual setup required - just use `_logger` normally
- **Searchable**: Query logs by `CorrelationId` in your logging system
- **Filterable**: Filter logs by correlation ID across all services  
- **Groupable**: Group related log entries for request tracing
- **Consistent**: Every log entry automatically includes correlation context

## Usage Examples

### Async Methods (Recommended)
```csharp
[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly ILogger<WeatherController> _logger;
    private readonly ICorrelationIdService _correlationIdService;

    public WeatherController(ILogger<WeatherController> logger, ICorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    [HttpGet]
    public async Task<IActionResult> GetWeatherAsync()
    {
        var correlationId = _correlationIdService.Get();
        
        _logger.LogInformation("Processing weather request");
        
        // Simulate async work
        await Task.Delay(100);
        
        var weather = new { Temperature = 72, Condition = "Sunny", CorrelationId = correlationId };
        
        _logger.LogInformation("Weather request completed");
        
        return Ok(weather);
    }
}
```

### Sync Methods
```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly ICorrelationIdService _correlationIdService;

    public UsersController(ILogger<UsersController> logger, ICorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    [HttpGet("{id}")]
    public IActionResult GetUser(int id)
    {
        var correlationId = _correlationIdService.Get();
        
        _logger.LogInformation("Getting user {UserId}", id);
        
        // Simulate work
        Thread.Sleep(50);
        
        var user = new { Id = id, Name = "John Doe", CorrelationId = correlationId };
        
        _logger.LogInformation("User retrieved successfully");
        
        return Ok(user);
    }
}
```

## HTTP Client Integration Features

### Automatic Correlation ID Propagation
- **Message Handler**: Automatically adds `X-Correlation-Id` header to all outgoing HTTP requests
- **Logging Integration**: Logs all HTTP requests/responses with correlation context
- **Error Handling**: Maintains correlation context even when HTTP calls fail
- **Thread Safety**: Works correctly with async/await and parallel HTTP calls

### Available HTTP Clients
- **ICorrelatedHttpClient**: High-level typed client with built-in JSON serialization
- **Named HttpClients**: Pre-configured clients for different scenarios
  - `CorrelationIdHttpClientNames.Default`: General purpose client
  - `CorrelationIdHttpClientNames.ExternalApi`: For external API calls
  - `CorrelationIdHttpClientNames.InternalService`: For internal service calls

### Correlation Flow Example
```
Incoming Request: GET /api/data
Header: X-Correlation-Id: user123abc

Your API logs:
[CorrelationId: user123abc] Processing request
[CorrelationId: user123abc] Sending HTTP GET request to https://external-api.com/data

External API receives:
Header: X-Correlation-Id: user123abc

External API logs (if using correlation ID):
[CorrelationId: user123abc] External API processing request

Your API logs:
[CorrelationId: user123abc] Received HTTP 200 response from https://external-api.com/data
[CorrelationId: user123abc] Request processing completed

Response: 200 OK
Header: X-Correlation-Id: user123abc
```

## How It Works

1. **Middleware**: `CorrelationIdMiddleware` intercepts all requests
   - Checks for `X-Correlation-Id` header
   - Generates new ID if missing (full GUID)
   - Sets correlation ID in response headers
   - Stores correlation ID in thread-local storage

2. **Logging**: Custom logger wrapper automatically adds correlation ID to log messages
   - **Message Format**: `[CorrelationId: 12345678-1234-1234-1234-123456789abc] Your log message`
   - **Structured Properties**: Adds `CorrelationId` as searchable properties
   - **Scoped Logging**: Uses `BeginScope()` to add correlation context to all nested log calls

3. **Method Tracking**: `CorrelationIdHelper` provides utilities to:
   - Track method execution with correlation ID context
   - Support both sync and async operations
   - Automatically capture calling method names

## Example API Flow

```
Request:  GET /api/weather
Header:   X-Correlation-Id: user123abc

Response: 200 OK
Header:   X-Correlation-Id: user123abc

Logs:
[CorrelationId: user123abc] Getting weather forecast
[CorrelationId: user123abc] Generating weather forecast data
```

If no correlation ID header is provided:

```
Request:  GET /api/weather
(no correlation header)

Response: 200 OK
Header:   X-Correlation-Id: a1b2c3d4-e5f6-7890-abcd-ef1234567890

Logs:
[CorrelationId: a1b2c3d4-e5f6-7890-abcd-ef1234567890] Getting weather forecast
[CorrelationId: a1b2c3d4-e5f6-7890-abcd-ef1234567890] Generating weather forecast data
```

## Configuration

No configuration required. The implementation is intentionally simple and opinionated:
- Uses `X-Correlation-Id` header (fixed name)
- Generates full GUID correlation IDs
- Automatically applies to all API flows
- Automatically adds to all logs
- No flexibility options by design
