# AspireApp1.CorrelationId.AzureFunctions

A comprehensive correlation ID tracking library for Azure Functions with support for **multiple trigger types** and **configurable additional headers**, adapted from the ASP.NET Core version.

## Features

- **Multi-Trigger Support**: HTTP, Queue, Service Bus, Event Hub, Timer, and Blob triggers
- **Automatic Header Tracking**: Tracks `X-Correlation-Id` and configurable additional headers on HTTP-triggered functions
- **Additional Headers Support**: Capture custom headers like X-Event-Id, X-User-Id, X-Tenant-Id alongside correlation ID
- **Message-Based Correlation**: Extracts correlation IDs from queue messages, Service Bus messages, and Event Hub events
- **Auto-Generation**: Generates new correlation ID if not present
- **Configurable Extraction**: Flexible configuration for different trigger types
- **Automatic Response Headers**: Adds correlation ID and additional headers to HTTP response headers
- **Structured Logging**: Adds all captured headers as searchable properties in Azure Functions logs
- **Thread-Safe**: Uses `AsyncLocal<T>` for thread-safe header storage
- **Base Class Support**: Provides base classes for different trigger types
- **Helper Methods**: Utility methods for manual correlation tracking
- **Activity Integration**: Works with distributed tracing and Application Insights

## Quick Start

### 1. Basic Setup (All Triggers)

```csharp
using Microsoft.Extensions.Hosting;
using AspireApp1.CorrelationId.AzureFunctions;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Basic setup with default configuration
        services.AddCorrelationId();
    })
    .Build();

host.Run();
```

### 2. Additional Headers Configuration

```csharp
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Configure additional headers alongside correlation ID
        services.AddCorrelationId(options =>
        {
            // Configure additional headers to capture
            options.AdditionalHeaders.AddRange(new[]
            {
                "X-Event-Id",           // Custom event tracking header
                "X-User-Id",            // User identifier for request tracking
                "X-Request-Source",     // Source system identifier
                "X-Tenant-Id",          // Multi-tenant identifier
                "X-Session-Id"          // Session tracking
            });

            // Add captured headers to HTTP response headers for client tracking
            options.AddAdditionalHeadersToResponse = true;

            // Configure correlation ID header name (default: "X-Correlation-Id")
            options.HeaderName = "X-Correlation-Id";

            // Control auto-generation (default: true)
            options.AutoGenerate = true;

            // Add correlation ID to HTTP response headers (default: true)
            options.AddToResponseHeaders = true;
        });
    })
    .Build();

host.Run();
```

### 3. Advanced Setup with HTTP Client Integration

```csharp
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Advanced setup with HTTP client integration and additional headers
        services.AddCorrelationIdWithHttpClient(options =>
        {
            // Configure additional headers
            options.AdditionalHeaders.AddRange(new[]
            {
                "X-Event-Id",
                "X-User-Id", 
                "X-Request-Source"
            });
            
            options.AddAdditionalHeadersToResponse = true;
            options.AutoGenerate = true;
            options.AddToResponseHeaders = true;

            // HTTP trigger settings
            options.Triggers.Http.UseQueryParameter = true;
            options.Triggers.Http.QueryParameterName = "correlationId";

            // Queue trigger settings
            options.Triggers.Queue.ParseFromMessageBody = true;
            options.Triggers.Queue.MessageBodyPropertyPath = "correlationId";

            // Service Bus trigger settings
            options.Triggers.ServiceBus.UseMessageCorrelationId = true;
            options.Triggers.ServiceBus.UseApplicationProperties = true;

            // Event Hub trigger settings
            options.Triggers.EventHub.UseEventProperties = true;
            options.Triggers.EventHub.ParseFromEventBody = true;

            // Timer trigger settings
            options.Triggers.Timer.IncludeScheduleInfo = true;

            // Blob trigger settings
            options.Triggers.Blob.UseBlobMetadata = true;
            options.Triggers.Blob.GenerateFromBlobPath = false;
        });
    })
    .Build();
```

### 4. Accessing Additional Headers Programmatically

```csharp
public class MyFunction : CorrelatedHttpFunction
{
    public MyFunction(ILoggerFactory loggerFactory, IEnhancedCorrelationIdService correlationIdService)
        : base(loggerFactory.CreateLogger<MyFunction>(), correlationIdService)
    {
    }

    [Function("ProcessWithHeaders")]
    public async Task<HttpResponseData> ProcessWithHeaders([HttpTrigger] HttpRequestData req)
    {
        return await ExecuteWithCorrelationAsync(req, async () =>
        {
            // All logs automatically include all captured headers
            Logger.LogInformation("Processing request with headers");

            // Get all captured headers (correlation ID + additional headers)
            var allHeaders = CorrelationIdService.CapturedHeaders;
            
            // Get specific headers
            var eventId = CorrelationIdService.GetHeader("X-Event-Id");
            var userId = CorrelationIdService.GetHeader("X-User-Id");
            var correlationId = CorrelationIdService.CorrelationId;
            
            // Set additional headers programmatically
            CorrelationIdService.SetAdditionalHeaders(new Dictionary<string, string>
            {
                { "X-Processing-Stage", "business-logic" },
                { "X-Function-Instance", Environment.MachineName }
            });
            
            Logger.LogInformation("Request completed with headers: {Headers}", 
                string.Join(", ", allHeaders.Select(h => $"{h.Key}={h.Value}")));
            
            var response = new 
            { 
                Result = "Success", 
                CorrelationId = correlationId,
                EventId = eventId,
                UserId = userId,
                AllHeaders = allHeaders
            };

            return await CreateJsonResponseAsync(req, response);
        });
    }
}
```

## ⚡ **Important: Wrapping Required ONLY ONCE Per Function**

**Key Point:** You only need to establish correlation context **once** at the function entry point. After that, correlation ID flows automatically through **everything**:

### ✅ **Automatic Flow After Initial Setup**
```csharp
public class OrderFunction : CorrelatedHttpFunction
{
    [Function("ProcessOrder")]
    public async Task<HttpResponseData> ProcessOrder([HttpTrigger] HttpRequestData req)
    {
        // ⚠️ WRAP ONCE - Only at function entry point
        return await ExecuteWithCorrelationAsync(req, async () =>
        {
            // ✅ From here, correlation ID flows EVERYWHERE automatically:
            
            Logger.LogInformation("Processing order"); // ✅ Automatic
            
            // ✅ All nested methods - automatic
            await ValidateOrder();
            await ProcessPayment();
            await UpdateInventory();
            
            // ✅ Parallel operations - automatic
            await Task.WhenAll(
                SendCustomerEmail(),
                UpdateAnalytics(),
                NotifyWarehouse()
            );
            
            // ✅ Even background tasks within function - automatic
            var backgroundTask = Task.Run(async () => {
                Logger.LogDebug("Background cleanup"); // ✅ Correlation flows here!
                await CleanupTempData();
            });
            
            // ✅ HTTP calls - automatic correlation headers
            await _httpClient.PostAsync("https://api.example.com/notify", content);
            
            return await CreateJsonResponseAsync(req, new { Status = "Success" });
        });
        // ⚠️ This wrapper above is the ONLY wrapping needed for entire function
    }
    
    // ✅ All these methods automatically have correlation - NO additional wrapping
    private async Task ValidateOrder()
    {
        Logger.LogDebug("Validating..."); // ✅ Correlation automatic
        await CallValidationService();    // ✅ Correlation automatic
        await CheckInventory();           // ✅ Correlation automatic
    }
    
    private async Task ProcessPayment()
    {
        Logger.LogDebug("Processing payment..."); // ✅ Correlation automatic
        // No matter how deep the call stack goes - correlation flows automatically
    }
}
```

### 🎯 **What Flows Automatically (No Additional Wrapping)**
- ✅ **All nested method calls** (no matter how deep)
- ✅ **All async operations** (`await`, `Task.Run`, `Task.WhenAll`)
- ✅ **HTTP client calls** (automatic correlation headers)
- ✅ **Parallel operations** and background tasks within the function
- ✅ **All logging** (automatic correlation ID inclusion)
- ✅ **Service calls** and database operations
- ✅ **Error handling and exceptions**

**Bottom Line:** Establish correlation context **once** at function entry → Everything else flows automatically!

## Usage Examples

### HTTP Trigger (Option A: Base Class)

```csharp
public class WeatherFunction : CorrelatedHttpFunction
{
    public WeatherFunction(ILogger<WeatherFunction> logger, IEnhancedCorrelationIdService correlationIdService) 
        : base(logger, correlationIdService) { }

    [Function("GetWeather")]
    public async Task<HttpResponseData> GetWeatherAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        return await ExecuteWithCorrelationAsync(req, async () =>
        {
            Logger.LogInformation("Getting weather forecast");
            var weather = new { Temperature = 72, Condition = "Sunny" };
            return await CreateJsonResponseAsync(req, weather);
        });
    }
}
```

### Queue Trigger (Option A: Base Class)

```csharp
public class OrderProcessor : CorrelatedQueueFunction
{
    public OrderProcessor(ILogger<OrderProcessor> logger, IEnhancedCorrelationIdService correlationIdService) 
        : base(logger, correlationIdService) { }

    [Function("ProcessOrder")]
    public async Task ProcessOrderAsync(
        [QueueTrigger("orders")] string orderMessage,
        FunctionContext context)
    {
        await ExecuteQueueFunctionAsync(orderMessage, context, async () =>
        {
            Logger.LogInformation("Processing order: {Order}", orderMessage);
            // Your order processing logic
            await Task.Delay(100);
        });
    }
}
```

### Service Bus Trigger (Option A: Base Class)

```csharp
public class EventProcessor : CorrelatedServiceBusFunction
{
    public EventProcessor(ILogger<EventProcessor> logger, IEnhancedCorrelationIdService correlationIdService) 
        : base(logger, correlationIdService) { }

    [Function("ProcessEvent")]
    public async Task ProcessEventAsync(
        [ServiceBusTrigger("events", "processor")] string eventMessage,
        FunctionContext context)
    {
        await ExecuteServiceBusFunctionAsync(eventMessage, context, async () =>
        {
            Logger.LogInformation("Processing event: {Event}", eventMessage);
            // Your event processing logic
            await Task.Delay(150);
        });
    }
}
```

### Timer Trigger (Option A: Base Class)

```csharp
public class ScheduledTask : CorrelatedTimerFunction
{
    public ScheduledTask(ILogger<ScheduledTask> logger, IEnhancedCorrelationIdService correlationIdService) 
        : base(logger, correlationIdService) { }

    [Function("DailyCleanup")]
    public async Task DailyCleanupAsync(
        [TimerTrigger("0 0 2 * * *")] object timerInfo, // 2 AM daily
        FunctionContext context)
    {
        await ExecuteTimerFunctionAsync(timerInfo, context, async () =>
        {
            Logger.LogInformation("Running daily cleanup task");
            // Your cleanup logic
            await Task.Delay(5000);
        });
    }
}
```

### Manual Approach (Option B: Manual Control)

```csharp
public class FlexibleFunction
{
    private readonly ILogger<FlexibleFunction> _logger;
    private readonly IEnhancedCorrelationIdService _correlationIdService;

    public FlexibleFunction(ILogger<FlexibleFunction> logger, IEnhancedCorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    [Function("FlexibleHttpTrigger")]
    public async Task<HttpResponseData> HttpTriggerAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        // Manual correlation ID initialization
        var correlationId = _correlationIdService.InitializeForHttpTrigger(req);
        var correlatedLogger = new CorrelationIdLogger(_logger, _correlationIdService);

        correlatedLogger.LogInformation("Processing HTTP request");
        
        // Your logic here
        var result = new { Message = "Success", CorrelationId = correlationId };
        
        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        response.Headers.Add("X-Correlation-Id", correlationId);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(System.Text.Json.JsonSerializer.Serialize(result));
        
        return response;
    }

    [Function("FlexibleQueueTrigger")]
    public async Task QueueTriggerAsync(
        [QueueTrigger("messages")] string queueMessage,
        FunctionContext context)
    {
        // Manual correlation ID initialization for queue
        var correlationId = _correlationIdService.InitializeForQueueTrigger(queueMessage, context);
        var correlatedLogger = new CorrelationIdLogger(_logger, _correlationIdService);

        correlatedLogger.LogInformation("Processing queue message: {Message}", queueMessage);
        
        // Your queue processing logic
        await Task.Delay(100);
        
        correlatedLogger.LogInformation("Queue processing completed");
    }
}
```

## Trigger-Specific Correlation ID Sources

### HTTP Triggers
- **Headers**: `X-Correlation-Id` (or custom header name)
- **Query Parameters**: `?correlationId=abc123` (optional)
- **Auto-Generation**: If not found in headers/query

### Queue Triggers  
- **Message Properties**: Custom properties in queue message
- **Message Body**: JSON property path extraction
- **Auto-Generation**: If not found in message

### Service Bus Triggers
- **Message CorrelationId**: Built-in Service Bus correlation ID
- **Application Properties**: Custom properties in message
- **Auto-Generation**: If not found in message properties

### Event Hub Triggers
- **Event Properties**: Custom properties in event data
- **Event Body**: JSON property path extraction  
- **Auto-Generation**: If not found in event

### Timer Triggers
- **Always Generated**: New correlation ID for each execution
- **Schedule Info**: Includes timer schedule in context

### Blob Triggers
- **Blob Metadata**: Custom metadata on blob
- **Blob Path**: Generate from blob name/path (optional)
- **Auto-Generation**: If not found in metadata

## Configuration Options

```csharp
services.AddCorrelationId(options =>
{
    // Global Configuration
    options.HeaderName = "X-Correlation-Id";           // HTTP header name
    options.AutoGenerate = true;                       // Generate if missing
    options.AddToResponseHeaders = true;               // Add to HTTP responses
    options.LogFunctionExecution = true;               // Log start/end

    // HTTP Trigger Configuration
    options.Triggers.Http.Enabled = true;
    options.Triggers.Http.UseQueryParameter = false;   // Check query params
    options.Triggers.Http.QueryParameterName = "correlationId";

    // Queue Trigger Configuration  
    options.Triggers.Queue.Enabled = true;
    options.Triggers.Queue.UseMessageProperties = true;
    options.Triggers.Queue.MessagePropertyName = "CorrelationId";
    options.Triggers.Queue.ParseFromMessageBody = false;
    options.Triggers.Queue.MessageBodyPropertyPath = "correlationId";

    // Service Bus Trigger Configuration
    options.Triggers.ServiceBus.Enabled = true;
    options.Triggers.ServiceBus.UseMessageCorrelationId = true;
    options.Triggers.ServiceBus.UseApplicationProperties = true;
    options.Triggers.ServiceBus.ApplicationPropertyName = "CorrelationId";

    // Event Hub Trigger Configuration
    options.Triggers.EventHub.Enabled = true;
    options.Triggers.EventHub.UseEventProperties = true;
    options.Triggers.EventHub.EventPropertyName = "CorrelationId";
    options.Triggers.EventHub.ParseFromEventBody = false;
    options.Triggers.EventHub.EventBodyPropertyPath = "correlationId";

    // Timer Trigger Configuration
    options.Triggers.Timer.Enabled = true;
    options.Triggers.Timer.GenerateForEachExecution = true;
    options.Triggers.Timer.IncludeScheduleInfo = true;

    // Blob Trigger Configuration
    options.Triggers.Blob.Enabled = true;
    options.Triggers.Blob.UseBlobMetadata = true;
    options.Triggers.Blob.MetadataKeyName = "CorrelationId";
    options.Triggers.Blob.GenerateFromBlobPath = false;
});
```

## Automatic Structured Logging

All logging automatically includes correlation ID properties across all trigger types:

```csharp
Logger.LogInformation("Processing order {OrderId}", orderId);

// Results in Azure Functions log with:
// Message: "[CorrelationId: 12345678-1234-1234-1234-123456789abc] Processing order 12345"
// Properties: 
//   - CorrelationId: "12345678-1234-1234-1234-123456789abc"
//   - OrderId: 12345
//   - TriggerType: "Queue" (or Http, ServiceBus, etc.)
//   - Source: "ProcessOrder"
```

## Complete Usage Examples by Approach

### Option A: Using Base Classes (Recommended)

#### Async HTTP Function
```csharp
public class WeatherFunction : CorrelatedHttpFunction
{
    public WeatherFunction(ILogger<WeatherFunction> logger, IEnhancedCorrelationIdService correlationIdService) 
        : base(logger, correlationIdService) { }

    [Function("GetWeatherAsync")]
    public async Task<HttpResponseData> GetWeatherAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        return await ExecuteWithCorrelationAsync(req, async () =>
        {
            Logger.LogInformation("Getting weather forecast");
            
            // Simulate async work
            await Task.Delay(100);
            
            var weather = new { Temperature = 72, Condition = "Sunny" };
            return await CreateJsonResponseAsync(req, weather);
        });
    }
}
```

#### Sync HTTP Function
```csharp
public class UserFunction : CorrelatedHttpFunction
{
    public UserFunction(ILogger<UserFunction> logger, IEnhancedCorrelationIdService correlationIdService) 
        : base(logger, correlationIdService) { }

    [Function("GetUser")]
    public async Task<HttpResponseData> GetUser(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users/{id}")] HttpRequestData req)
    {
        return await ExecuteWithCorrelationAsync(req, async () =>
        {
            Logger.LogInformation("Getting user");
            
            // Simulate sync work
            Thread.Sleep(50);
            
            var user = new { Id = 123, Name = "John Doe" };
            return await CreateJsonResponseAsync(req, user);
        });
    }
}
```

#### Async Queue Function
```csharp
public class OrderProcessor : CorrelatedQueueFunction
{
    public OrderProcessor(ILogger<OrderProcessor> logger, IEnhancedCorrelationIdService correlationIdService) 
        : base(logger, correlationIdService) { }

    [Function("ProcessOrderAsync")]
    public async Task ProcessOrderAsync(
        [QueueTrigger("orders")] string orderMessage,
        FunctionContext context)
    {
        await ExecuteQueueFunctionAsync(orderMessage, context, async () =>
        {
            Logger.LogInformation("Processing order: {Order}", orderMessage);
            
            // Simulate async processing
            await Task.Delay(200);
            
            Logger.LogInformation("Order processing completed");
        });
    }
}
```

#### Sync Queue Function
```csharp
public class NotificationProcessor : CorrelatedQueueFunction
{
    public NotificationProcessor(ILogger<NotificationProcessor> logger, IEnhancedCorrelationIdService correlationIdService) 
        : base(logger, correlationIdService) { }

    [Function("ProcessNotification")]
    public async Task ProcessNotificationAsync(
        [QueueTrigger("notifications")] string notification,
        FunctionContext context)
    {
        await ExecuteQueueFunctionAsync(notification, context, async () =>
        {
            Logger.LogInformation("Processing notification: {Notification}", notification);
            
            // Simulate sync processing
            Thread.Sleep(100);
            
            Logger.LogInformation("Notification sent");
        });
    }
}
```

### Option B: Manual Approach

#### Async Manual Function
```csharp
public class FlexibleAsyncFunction
{
    private readonly ILogger<FlexibleAsyncFunction> _logger;
    private readonly IEnhancedCorrelationIdService _correlationIdService;

    public FlexibleAsyncFunction(ILogger<FlexibleAsyncFunction> logger, IEnhancedCorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    [Function("FlexibleAsyncHttpTrigger")]
    public async Task<HttpResponseData> HttpTriggerAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        // Manual correlation ID initialization
        var correlationId = _correlationIdService.InitializeForHttpTrigger(req);
        var correlatedLogger = new CorrelationIdLogger(_logger, _correlationIdService);

        correlatedLogger.LogInformation("Processing async HTTP request");
        
        // Simulate async work
        await Task.Delay(150);
        
        var result = new { Message = "Async Success", CorrelationId = correlationId };
        
        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        response.Headers.Add("X-Correlation-Id", correlationId);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(System.Text.Json.JsonSerializer.Serialize(result));
        
        return response;
    }
}
```

#### Sync Manual Function
```csharp
public class FlexibleSyncFunction
{
    private readonly ILogger<FlexibleSyncFunction> _logger;
    private readonly IEnhancedCorrelationIdService _correlationIdService;

    public FlexibleSyncFunction(ILogger<FlexibleSyncFunction> logger, IEnhancedCorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    [Function("FlexibleSyncQueueTrigger")]
    public async Task QueueTriggerAsync(
        [QueueTrigger("messages")] string queueMessage,
        FunctionContext context)
    {
        // Manual correlation ID initialization for queue
        var correlationId = _correlationIdService.InitializeForQueueTrigger(queueMessage, context);
        var correlatedLogger = new CorrelationIdLogger(_logger, _correlationIdService);

        correlatedLogger.LogInformation("Processing sync queue message: {Message}", queueMessage);
        
        // Simulate sync processing
        Thread.Sleep(75);
        
        correlatedLogger.LogInformation("Sync queue processing completed");
    }
}
```

## Integration with Application Insights

The correlation IDs automatically appear in Application Insights for all trigger types:
- **Custom Properties**: Filter and search by correlation ID
- **Distributed Tracing**: Cross-service correlation
- **Request Correlation**: Link related function executions
- **Trigger Context**: Additional metadata about trigger type and source

## Example Function Flows

### HTTP Trigger Flow
```
Request:  GET /api/GetWeather
Header:   X-Correlation-Id: user123abc

Response: 200 OK
Header:   X-Correlation-Id: user123abc

Logs:
[CorrelationId: user123abc] Function 'GetWeather' started - Trigger: Http
[CorrelationId: user123abc] Getting weather forecast
[CorrelationId: user123abc] Function 'GetWeather' completed successfully
```

### Queue Trigger Flow
```
Queue Message: {"orderId": 12345, "correlationId": "order456def"}

Logs:
[CorrelationId: order456def] Function 'ProcessOrder' started - Trigger: Queue
[CorrelationId: order456def] Processing order: {"orderId": 12345, "correlationId": "order456def"}
[CorrelationId: order456def] Function 'ProcessOrder' completed successfully
```

### Timer Trigger Flow
```
Timer: Daily cleanup at 2 AM

Logs:
[CorrelationId: timer789ghi] Function 'DailyCleanup' started - Trigger: Timer
[CorrelationId: timer789ghi] Running daily cleanup task
[CorrelationId: timer789ghi] Function 'DailyCleanup' completed successfully
```

## HTTP Client Integration

### Automatic Correlation ID Propagation
All HTTP clients automatically include the `X-Correlation-Id` header in outgoing requests:

```csharp
public class OrderProcessor : CorrelatedHttpFunction
{
    private readonly ICorrelatedHttpClient _httpClient;

    public OrderProcessor(
        ILogger<OrderProcessor> logger, 
        IEnhancedCorrelationIdService correlationIdService,
        ICorrelatedHttpClient httpClient) 
        : base(logger, correlationIdService) 
    {
        _httpClient = httpClient;
    }

    [Function("ProcessOrder")]
    public async Task<HttpResponseData> ProcessOrder(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        return await ExecuteWithCorrelationAsync(req, async () =>
        {
            Logger.LogInformation("Processing order request");
            
            // Automatically includes X-Correlation-Id header
            var paymentResponse = await _httpClient.PostAsJsonAsync(
                "https://payment-service.com/api/process", 
                new { amount = 100.00m });
            
            // Call inventory service with correlation ID  
            var inventoryResponse = await _httpClient.GetAsync(
                "https://inventory-service.com/api/stock/check");
            
            Logger.LogInformation("Order processing completed");
            
            return await CreateJsonResponseAsync(req, new { success = true });
        });
    }
}
```

### Named HTTP Clients
Configure different HTTP clients for various scenarios:

```csharp
// In Program.cs
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddCorrelationId();
        
        // Named HTTP clients with correlation
        services.AddHttpClient(CorrelationIdHttpClientNames.PaymentService, client =>
        {
            client.BaseAddress = new Uri("https://payment-service.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        services.AddHttpClient(CorrelationIdHttpClientNames.InventoryService, client =>
        {
            client.BaseAddress = new Uri("https://inventory-service.com/");
        });
    })
    .Build();

// In your function
public class OrderProcessor : CorrelatedQueueFunction
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OrderProcessor(
        ILogger<OrderProcessor> logger, 
        IEnhancedCorrelationIdService correlationIdService,
        IHttpClientFactory httpClientFactory) 
        : base(logger, correlationIdService) 
    {
        _httpClientFactory = httpClientFactory;
    }

    [Function("ProcessQueueOrder")]
    public async Task ProcessOrderAsync(
        [QueueTrigger("orders")] string orderMessage,
        FunctionContext context)
    {
        await ExecuteQueueFunctionAsync(orderMessage, context, async () =>
        {
            Logger.LogInformation("Processing order: {Order}", orderMessage);
            
            // Use named clients with automatic correlation
            var paymentClient = _httpClientFactory.CreateClient(CorrelationIdHttpClientNames.PaymentService);
            var inventoryClient = _httpClientFactory.CreateClient(CorrelationIdHttpClientNames.InventoryService);
            
            await paymentClient.PostAsJsonAsync("api/process", new { amount = 100.00m });
            await inventoryClient.GetAsync("api/stock/check");
        });
    }
}
```

### HTTP Client Class Names
```csharp
public static class CorrelationIdHttpClientNames
{
    public const string Default = "CorrelationId.Default";
    public const string PaymentService = "CorrelationId.PaymentService";
    public const string InventoryService = "CorrelationId.InventoryService";
    public const string NotificationService = "CorrelationId.NotificationService";
    public const string ExternalApi = "CorrelationId.ExternalApi";
    public const string InternalService = "CorrelationId.InternalService";
}
```

### Correlation Flow Example with Multiple Triggers
```
Queue Message: {"orderId": 12345, "correlationId": "order456def"}

Your Function logs:
[CorrelationId: order456def] Function 'ProcessOrder' started - Trigger: Queue
[CorrelationId: order456def] Processing order: {"orderId": 12345}
[CorrelationId: order456def] Sending HTTP POST request to https://payment-service.com/api/process

Payment Service receives:
Header: X-Correlation-Id: order456def

Payment Service logs (if using correlation ID):
[CorrelationId: order456def] Processing payment request

Your Function logs:
[CorrelationId: order456def] Received HTTP 200 response from https://payment-service.com/api/process
[CorrelationId: order456def] Function 'ProcessOrder' completed successfully
```

## Key Differences from ASP.NET Core Version

1. **Multi-Trigger Support**: Supports all Azure Functions trigger types, not just HTTP
2. **Trigger-Specific Extraction**: Different correlation ID sources per trigger type
3. **Function-Scoped**: Correlation ID managed per function execution context
4. **Configurable Sources**: Flexible configuration for different correlation ID sources
5. **Azure Functions Optimized**: Built specifically for Azure Functions runtime and logging
6. **HTTP Client Integration**: Automatic correlation ID propagation in outgoing HTTP calls
