# AspireApp1.CorrelationId

A simple and basic implementation of correlation ID tracking for ASP.NET Core applications.

## Features

- **Automatic Header Tracking**: Tracks `X-Correlation-Id` header on all incoming requests
- **Auto-Generation**: Generates new correlation ID if header is missing
- **Automatic Logging**: Adds correlation ID to all log entries (both message prefix and structured properties)
- **Structured Logging**: Adds correlation ID as custom properties for searchable metadata
- **Sync/Async Support**: Provides helpers for both synchronous and asynchronous method execution
- **Thread-Safe**: Uses `AsyncLocal<T>` for thread-safe correlation ID storage

## Usage

### 1. Add to Services

```csharp
builder.Services.AddCorrelationId();
```

### 2. Add to Request Pipeline

```csharp
// Add early in the pipeline, after UseExceptionHandler
app.UseCorrelationId();
```

### 3. Use in Controllers

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

    [HttpGet("async")]
    public async Task<IActionResult> GetAsync()
    {
        // For async methods - correlation ID automatically included in all logs
        var result = await CorrelationIdHelper.ExecuteWithCorrelationIdAsync(_correlationIdService, async () =>
        {
            _logger.LogDebug("Inside async operation"); // Automatically includes correlation ID
            await Task.Delay(100);
            return "Async Result";
        });

        return Ok(result);
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
