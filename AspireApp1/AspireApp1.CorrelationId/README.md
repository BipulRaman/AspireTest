# AspireApp1.CorrelationId

A **zero-boilerplate** correlation ID tracking library for ASP.NET Core applications with **automatic HTTP client integration** and **configurable additional headers** for comprehensive distributed tracing.

## Features

- **🚀 Zero Boilerplate**: No wrapper methods needed - correlation ID available everywhere automatically
- **🔄 Automatic Flow**: Correlation ID flows seamlessly through all async operations via `AsyncLocal<T>`
- **📨 Automatic Header Tracking**: Tracks `X-Correlation-Id` and configurable additional headers on all incoming requests
- **🎯 Auto-Generation**: Generates new correlation ID if header is missing
- **📝 Automatic Logging**: Adds all captured headers to log entries (both message prefix and structured properties)
- **🔍 Structured Logging**: Adds all headers as custom properties for searchable metadata
- **🌐 HTTP Client Integration**: Automatically propagates all headers to outgoing HTTP requests
- **🏷️ Named HTTP Clients**: Support for multiple configured HTTP clients with correlation
- **🔗 Distributed Tracing**: End-to-end correlation across microservices with custom headers
- **⚡ Thread-Safe**: Uses `AsyncLocal<T>` for thread-safe header storage
- **🎛️ Configurable Headers**: Support for additional custom headers like X-Event-Id, X-User-Id, etc.

## Usage

### 1. Basic Setup (Correlation ID only)

```csharp
// Simple setup - correlation ID only
builder.Services.AddCorrelationId();
app.UseCorrelationId();

// That's it! Correlation ID now available everywhere automatically
```

### 2. Basic Setup with Additional Headers

```csharp
// Configure additional headers without HTTP client integration
builder.Services.AddCorrelationId(options =>
{
    // Configure additional headers to capture and log
    options.AdditionalHeaders.AddRange(new[]
    {
        "X-Event-Id",           // Custom event tracking header
        "X-User-Id",            // User identifier for request tracking
        "X-Request-Source"      // Source system identifier
    });
    
    // Add captured headers to response for client tracking
    options.AddAdditionalHeadersToResponse = true;
});

app.UseCorrelationId();
```

### 3. Advanced Setup (With HTTP Client Integration - Recommended)

```csharp
// Add correlation ID with HTTP client support
builder.Services.AddCorrelationIdWithHttpClient();

// Configure middleware
app.UseCorrelationId();

// Now all HTTP calls automatically include correlation headers!
```

### 4. Full Configuration with HTTP Client Integration

```csharp
// Configure additional headers with HTTP client integration
builder.Services.AddCorrelationIdWithHttpClient(options =>
{
    // Configure additional headers to capture and log
    options.AdditionalHeaders.AddRange(new[]
    {
        "X-Event-Id",           // Custom event tracking header
        "X-User-Id",            // User identifier for request tracking
        "X-Request-Source",     // Source system identifier
        "X-Tenant-Id",          // Multi-tenant identifier
        "X-Session-Id"          // Session tracking
    });
    
    // Add captured headers to response for client tracking
    options.AddAdditionalHeadersToResponse = true;
    
    // Configure correlation ID header name (default: "X-Correlation-Id")
    options.CorrelationIdHeader = "X-Custom-Correlation-Id";
    
    // Control auto-generation (default: true)
    // When true: Automatically generates new correlation ID if request doesn't have one
    // When false: Only tracks correlation ID if provided in request headers
    options.AutoGenerate = true;
    
    // Add correlation ID to response headers (default: true)
    options.AddToResponseHeaders = true;
});

app.UseCorrelationId();
```

### 5. Custom HTTP Client Configuration

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

### 6. Accessing Additional Headers Programmatically

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
    public async Task<IActionResult> Get()
    {
        // All logs automatically include all captured headers - no wrapper needed!
        _logger.LogInformation("Processing request");
        
        // Get all captured headers (correlation ID + additional headers)
        var allHeaders = _correlationIdService.CapturedHeaders;
        
        // Get specific headers
        var eventId = _correlationIdService.GetHeader("X-Event-Id");
        var userId = _correlationIdService.GetHeader("X-User-Id");
        var correlationId = _correlationIdService.CorrelationId;
        
        // Set additional headers programmatically
        _correlationIdService.SetAdditionalHeaders(new Dictionary<string, string>
        {
            { "X-Processing-Stage", "business-logic" },
            { "X-Request-Priority", "high" }
        });
        
        _logger.LogInformation("Request completed with headers: {Headers}", 
            string.Join(", ", allHeaders.Select(h => $"{h.Key}={h.Value}")));
        
        return Ok(new 
        { 
            Result = "Success", 
            CorrelationId = correlationId,
            EventId = eventId,
            UserId = userId,
            AllHeaders = allHeaders
        });
    }
}
```

## Configuration Options Summary

Both `AddCorrelationId()` and `AddCorrelationIdWithHttpClient()` support the same configuration options:

| Method | HTTP Client Integration | Configuration Support | Use Case |
|--------|------------------------|----------------------|----------|
| `AddCorrelationId()` | ❌ No | ✅ Yes | Web apps, APIs without external HTTP calls |
| `AddCorrelationId(options => {})` | ❌ No | ✅ Yes | Web apps with additional headers, no HTTP calls |
| `AddCorrelationIdWithHttpClient()` | ✅ Yes | ❌ Default only | APIs with external HTTP calls, default config |
| `AddCorrelationIdWithHttpClient(options => {})` | ✅ Yes | ✅ Yes | APIs with external HTTP calls + custom headers |

**Choose your setup:**
- **Basic web app/API**: Use `AddCorrelationId()`
- **Need additional headers but no HTTP calls**: Use `AddCorrelationId(options => {})`
- **API calling other services**: Use `AddCorrelationIdWithHttpClient()`  
- **API with additional headers + HTTP calls**: Use `AddCorrelationIdWithHttpClient(options => {})`

## Configuration Options Explained

### CorrelationIdOptions Properties

```csharp
public class CorrelationIdOptions
{
    // Header name for correlation ID (default: "X-Correlation-Id")
    public string CorrelationIdHeader { get; set; } = "X-Correlation-Id";
    
    // List of additional headers to capture and track
    public List<string> AdditionalHeaders { get; set; } = new();
    
    // Auto-generate correlation ID if not provided in request (default: true)
    public bool AutoGenerate { get; set; } = true;
    
    // Add correlation ID to response headers (default: true)
    public bool AddToResponseHeaders { get; set; } = true;
    
    // Add captured additional headers to response headers (default: false)
    public bool AddAdditionalHeadersToResponse { get; set; } = false;
}
```

### AutoGenerate Behavior Examples

**AutoGenerate = true (Default):**
```
Incoming Request: GET /api/data
(no correlation header)

Middleware Action:
✅ Generates new correlation ID: "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
✅ Sets in context for logging and processing
✅ Adds to response headers

Response: 200 OK
Header: X-Correlation-Id: a1b2c3d4-e5f6-7890-abcd-ef1234567890

Logs: [CorrelationId: a1b2c3d4-e5f6-7890-abcd-ef1234567890] Processing request
```

**AutoGenerate = false:**
```
Incoming Request: GET /api/data
(no correlation header)

Middleware Action:
❌ No correlation ID generated
❌ CorrelationId remains null/empty
❌ No response header added

Response: 200 OK
(no correlation header)

Logs: Processing request (no correlation ID in logs)
```

**With AutoGenerate = false but header provided:**
```
Incoming Request: GET /api/data
Header: X-Correlation-Id: user123abc

Middleware Action:
✅ Uses provided correlation ID: "user123abc"
✅ Sets in context for logging
✅ Adds to response headers

Response: 200 OK
Header: X-Correlation-Id: user123abc

Logs: [CorrelationId: user123abc] Processing request
```

### When to Use AutoGenerate = false

Use `AutoGenerate = false` when:
- **Strict tracking only**: You only want to track requests that already have correlation IDs
- **Gateway scenarios**: External gateway handles correlation ID generation
- **Optional correlation**: Correlation ID is optional for your application
- **Performance**: Slight performance improvement by not generating GUIDs

Use `AutoGenerate = true` (default) when:
- **Complete tracing**: You want every request to have a correlation ID
- **Microservices**: Each service should generate IDs for requests without them
- **Debugging**: Easier to trace all requests, even those from tools/health checks

## Usage Examples

### 1. Basic Controller Usage (Automatic - No Wrappers Needed!)

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
    public async Task<IActionResult> Get()
    {
        // All logs automatically include correlation ID - no wrapper needed!
        _logger.LogInformation("Processing request");
        
        // Correlation ID flows automatically through async operations
        await SomeAsyncWork();
        
        // Get correlation ID anytime
        var correlationId = _correlationIdService.CorrelationId;
        
        _logger.LogInformation("Request completed");
        return Ok(new { Result = "Success", CorrelationId = correlationId });
    }
    
    private async Task SomeAsyncWork()
    {
        // Correlation ID automatically available in nested methods
        _logger.LogDebug("Doing async work");
        await Task.Delay(100);
        _logger.LogDebug("Async work completed");
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

### Async Methods (Automatic - Recommended)
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
        // Correlation ID automatically available - no wrapper needed!
        var correlationId = _correlationIdService.CorrelationId;
        
        _logger.LogInformation("Processing weather request");
        
        // Correlation ID flows automatically through async operations
        await Task.Delay(100);
        await ProcessWeatherDataAsync();
        
        var weather = new { Temperature = 72, Condition = "Sunny", CorrelationId = correlationId };
        
        _logger.LogInformation("Weather request completed");
        
        return Ok(weather);
    }
    
    private async Task ProcessWeatherDataAsync()
    {
        // Correlation ID automatically available in all nested methods
        _logger.LogDebug("Processing weather data");
        await Task.Delay(50);
        _logger.LogDebug("Weather data processed");
    }
}
```

### When You DO Need Helper Methods (Rare Cases)
The `CorrelationIdHelper` methods are only needed for these specific scenarios:

```csharp
[ApiController]
[Route("api/[controller]")]
public class BackgroundTaskController : ControllerBase
{
    private readonly ICorrelationIdService _correlationIdService;
    private readonly ILogger<BackgroundTaskController> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BackgroundTaskController(
        ICorrelationIdService correlationIdService, 
        ILogger<BackgroundTaskController> logger,
        IServiceProvider serviceProvider)
    {
        _correlationIdService = correlationIdService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    [HttpPost("process-order")]
    public async Task<IActionResult> ProcessOrder([FromBody] OrderRequest request)
    {
        _logger.LogInformation("Starting order processing for Order ID: {OrderId}", request.OrderId);

        // ✅ Normal async operations - correlation ID flows automatically
        await ValidateOrderAsync(request);
        await SaveOrderAsync(request);

        // ⚠️ Background task - needs explicit correlation context
        Task.Run(() => CorrelationIdHelper.ExecuteWithCorrelationId(_correlationIdService, () =>
        {
            // This runs on a background thread pool thread
            _logger.LogInformation("Processing order fulfillment in background for Order ID: {OrderId}", request.OrderId);
            
            // Heavy processing that doesn't block the request
            ProcessOrderFulfillment(request.OrderId);
            SendOrderConfirmationEmail(request.CustomerEmail);
            UpdateInventorySystem(request.Items);
            
            _logger.LogInformation("Background order processing completed for Order ID: {OrderId}", request.OrderId);
        }));

        _logger.LogInformation("Order processing initiated for Order ID: {OrderId}", request.OrderId);
        return Ok(new { OrderId = request.OrderId, Status = "Processing", Message = "Order processing started" });
    }

    [HttpPost("schedule-report")]
    public IActionResult ScheduleReport([FromBody] ReportRequest request)
    {
        _logger.LogInformation("Scheduling report generation: {ReportType}", request.ReportType);

        // ⚠️ Timer callbacks need explicit correlation context
        var timer = new System.Timers.Timer(TimeSpan.FromMinutes(request.DelayMinutes).TotalMilliseconds);
        timer.AutoReset = false;
        timer.Elapsed += (sender, e) => CorrelationIdHelper.ExecuteWithCorrelationId(_correlationIdService, () =>
        {
            _logger.LogInformation("Timer triggered - generating scheduled report: {ReportType}", request.ReportType);
            
            GenerateReport(request.ReportType, request.Parameters);
            
            _logger.LogInformation("Scheduled report generation completed: {ReportType}", request.ReportType);
            timer.Dispose();
        });
        
        timer.Start();
        return Ok(new { Message = $"Report scheduled to run in {request.DelayMinutes} minutes" });
    }

    [HttpPost("batch-process")]
    public async Task<IActionResult> BatchProcess([FromBody] BatchRequest request)
    {
        _logger.LogInformation("Starting batch processing for {ItemCount} items", request.Items.Count);

        // ⚠️ Parallel background processing with correlation context
        var tasks = request.Items.Select(item => 
            Task.Run(async () => await CorrelationIdHelper.ExecuteWithCorrelationIdAsync(_correlationIdService, async () =>
            {
                _logger.LogInformation("Processing batch item: {ItemId}", item.Id);
                
                // Each background task maintains correlation context
                await ProcessBatchItemAsync(item);
                await UpdateProgressAsync(item.Id, "Completed");
                
                _logger.LogInformation("Batch item completed: {ItemId}", item.Id);
                return item.Id;
            }))
        ).ToArray();

        // ✅ Awaiting tasks - correlation ID flows normally
        var completedItems = await Task.WhenAll(tasks);
        
        _logger.LogInformation("Batch processing completed for {CompletedCount} items", completedItems.Length);
        return Ok(new { CompletedItems = completedItems, Message = "Batch processing completed" });
    }

    [HttpPost("queue-message")]
    public IActionResult QueueMessage([FromBody] MessageRequest request)
    {
        _logger.LogInformation("Queuing message for processing: {MessageType}", request.MessageType);

        // ⚠️ Simulating message queue processing - needs correlation context
        ThreadPool.QueueUserWorkItem(_ => CorrelationIdHelper.ExecuteWithCorrelationId(_correlationIdService, () =>
        {
            _logger.LogInformation("Processing queued message: {MessageType}", request.MessageType);
            
            // Simulate message processing
            Thread.Sleep(1000);
            ProcessMessage(request);
            
            _logger.LogInformation("Queued message processed: {MessageType}", request.MessageType);
        }));

        return Ok(new { Message = "Message queued for processing" });
    }

    // ✅ Normal async methods - correlation ID flows automatically
    private async Task ValidateOrderAsync(OrderRequest request)
    {
        _logger.LogDebug("Validating order: {OrderId}", request.OrderId);
        await Task.Delay(100); // Simulate validation
        _logger.LogDebug("Order validation completed: {OrderId}", request.OrderId);
    }

    private async Task SaveOrderAsync(OrderRequest request)
    {
        _logger.LogDebug("Saving order to database: {OrderId}", request.OrderId);
        await Task.Delay(200); // Simulate database save
        _logger.LogDebug("Order saved: {OrderId}", request.OrderId);
    }

    // Methods called from background tasks - correlation ID available via helper
    private void ProcessOrderFulfillment(int orderId)
    {
        _logger.LogInformation("Processing fulfillment for order: {OrderId}", orderId);
        Thread.Sleep(2000); // Simulate heavy processing
    }

    private void SendOrderConfirmationEmail(string email)
    {
        _logger.LogInformation("Sending confirmation email to: {Email}", email);
        Thread.Sleep(500); // Simulate email sending
    }

    private void UpdateInventorySystem(List<OrderItem> items)
    {
        _logger.LogInformation("Updating inventory for {ItemCount} items", items.Count);
        Thread.Sleep(1000); // Simulate inventory update
    }

    private void GenerateReport(string reportType, Dictionary<string, object> parameters)
    {
        _logger.LogInformation("Generating report: {ReportType}", reportType);
        Thread.Sleep(3000); // Simulate report generation
    }

    private async Task ProcessBatchItemAsync(BatchItem item)
    {
        _logger.LogDebug("Processing batch item: {ItemId}", item.Id);
        await Task.Delay(500); // Simulate async processing
    }

    private async Task UpdateProgressAsync(int itemId, string status)
    {
        _logger.LogDebug("Updating progress for item {ItemId}: {Status}", itemId, status);
        await Task.Delay(100); // Simulate progress update
    }

    private void ProcessMessage(MessageRequest request)
    {
        _logger.LogInformation("Processing message: {MessageId}", request.MessageId);
        Thread.Sleep(800); // Simulate message processing
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
   - Stores correlation ID in thread-local storage using `AsyncLocal<T>`

2. **Automatic Context Flow**: Correlation ID flows automatically through your entire request
   - **AsyncLocal<T>**: Ensures correlation ID is available in all async operations
   - **No Wrappers Needed**: Just use `_correlationIdService.CorrelationId` anywhere
   - **Thread-Safe**: Works correctly with parallel async operations
   - **Nested Methods**: Correlation ID available in all nested method calls

3. **Logging**: Custom logger wrapper automatically adds correlation ID to log messages
   - **Message Format**: `[CorrelationId: 12345678-1234-1234-1234-123456789abc] Your log message`
   - **Structured Properties**: Adds `CorrelationId` as searchable properties
   - **Scoped Logging**: Uses `BeginScope()` to add correlation context to all nested log calls

4. **HTTP Client Integration**: Automatically propagates correlation ID to outgoing requests
   - **Message Handler**: Adds `X-Correlation-Id` header to all HTTP calls
   - **Named Clients**: Works with all configured HTTP clients
   - **Error Handling**: Maintains correlation context even when HTTP calls fail

## When Helper Methods Are Needed

The `CorrelationIdHelper.ExecuteWithCorrelationId*` methods are **only needed** for:
- **Background Tasks**: `Task.Run()`, `ThreadPool.QueueUserWorkItem()`
- **New Threads**: `new Thread()` or similar
- **Timer Callbacks**: `System.Timers.Timer` events
- **Message Queues**: Processing outside HTTP request context

For **normal controller operations**, the correlation ID is **automatically available** everywhere!

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

**Zero configuration required!** The implementation is intentionally simple and opinionated:
- Uses `X-Correlation-Id` header (fixed name)
- Generates full GUID correlation IDs automatically
- Automatically applies to all API flows via middleware
- Automatically adds correlation ID to all logs
- Automatically propagates to HTTP client calls
- No configuration options by design for maximum simplicity

## Key Benefits

✅ **Zero Boilerplate**: No wrapper methods needed in controllers
✅ **Automatic Flow**: Correlation ID available everywhere automatically  
✅ **Thread-Safe**: Works with async/await and parallel operations
✅ **HTTP Integration**: Automatic header propagation to outgoing calls
✅ **Logging Integration**: All logs automatically include correlation ID
✅ **Error Handling**: Correlation context maintained during exceptions
✅ **Distributed Tracing**: End-to-end correlation across microservices
