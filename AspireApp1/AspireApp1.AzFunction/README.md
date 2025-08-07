# AspireApp1.AzFunction

Azure Functions project demonstrating the **AspireApp1.CorrelationId.AzureFunctions** package functionality with custom correlation ID headers and additional headers support.

## Features Demonstrated

- ✅ **Custom Correlation ID Header**: Uses `X-Custom-Correlation-Id` instead of default
- ✅ **Additional Headers Capture**: Tracks `X-User-Id`, `X-Event-Id`, `X-Request-Source`, `X-Tenant-Id`
- ✅ **HTTP Client Propagation**: All headers automatically flow to external API calls
- ✅ **Response Header Injection**: Returns captured headers in HTTP responses
- ✅ **Structured Logging**: All log entries include correlation context
- ✅ **Programmatic Header Management**: Set headers dynamically during processing

## Function Endpoints

### Demo Functions

| Endpoint | Method | Description |
|----------|---------|-------------|
| `/api/demo/basic` | GET | Basic correlation ID demonstration |
| `/api/demo/headers` | GET | Additional headers capture demo |
| `/api/demo/http-propagation` | GET | HTTP client header propagation demo |
| `/api/demo/programmatic` | POST | Programmatic header manipulation demo |
| `/api/demo/workflow` | POST | Complex workflow with multiple stages |
| `/api/health` | GET | Health check and configuration status |

### Business Functions

| Endpoint | Method | Description |
|----------|---------|-------------|
| `/api/weather` | GET | Weather forecast with correlation tracking |
| `/api/weather/external` | GET | Weather with external API calls |
| `/api/weather` | POST | Update weather data |
| `/api/users/{userId}` | GET | Get user profile |
| `/api/users` | POST | Create new user |
| `/api/users/batch` | POST | Batch user processing |
| `/api/orders` | POST | Process new order |
| `/api/orders/{orderId}/status` | GET | Get order status |
| `/api/orders/{orderId}` | DELETE | Cancel order |

## Testing Examples

### 1. Basic Correlation ID (Auto-Generation)

```bash
# No correlation ID provided - will auto-generate
curl -X GET "http://localhost:7071/api/demo/basic"
```

**Response includes:**
- Auto-generated `X-Custom-Correlation-Id`
- Confirmation that ID was auto-generated
- All captured headers

### 2. Custom Correlation ID

```bash
# Provide custom correlation ID
curl -X GET "http://localhost:7071/api/demo/basic" \
  -H "X-Custom-Correlation-Id: my-custom-id-123"
```

**Response includes:**
- Your custom correlation ID
- Confirmation that ID was provided (not auto-generated)

### 3. Additional Headers Capture

```bash
# Send request with additional headers
curl -X GET "http://localhost:7071/api/demo/headers" \
  -H "X-Custom-Correlation-Id: test-123" \
  -H "X-User-Id: user-456" \
  -H "X-Event-Id: event-789" \
  -H "X-Tenant-Id: tenant-abc" \
  -H "X-Request-Source: mobile-app"
```

**Response includes:**
- All captured headers
- Individual header values
- Headers will also be in response headers

### 4. HTTP Client Propagation

```bash
# Test HTTP client header propagation
curl -X GET "http://localhost:7071/api/demo/http-propagation" \
  -H "X-Custom-Correlation-Id: propagation-test" \
  -H "X-User-Id: user-123" \
  -H "X-Tenant-Id: tenant-xyz"
```

**Response shows:**
- Headers sent to external API
- External service's response (showing it received our headers)
- Proof that custom correlation ID header is propagated

### 5. Programmatic Headers

```bash
# Test programmatic header setting
curl -X POST "http://localhost:7071/api/demo/programmatic" \
  -H "X-Custom-Correlation-Id: programmatic-test" \
  -H "X-User-Id: user-999" \
  -H "Content-Type: application/json" \
  -d '{"test": "data"}'
```

**Response shows:**
- Initial headers from request
- Programmatically added headers
- Final combined headers
- HTTP call result with all headers propagated

### 6. Complex Workflow

```bash
# Test complex multi-stage workflow
curl -X POST "http://localhost:7071/api/demo/workflow" \
  -H "X-Custom-Correlation-Id: workflow-123" \
  -H "X-User-Id: user-555" \
  -H "Content-Type: application/json" \
  -d '{"workflowType": "demo"}'
```

**Response shows:**
- Each workflow stage with headers at that point
- How headers evolve through the workflow
- Multiple HTTP calls with full context propagation

### 7. Business Function Example

```bash
# Test order processing with correlation tracking
curl -X POST "http://localhost:7071/api/orders" \
  -H "X-Custom-Correlation-Id: order-123" \
  -H "X-User-Id: customer-456" \
  -H "X-Request-Source: web-portal" \
  -H "Content-Type: application/json" \
  -d '{"product": "laptop", "quantity": 1, "price": 999.99}'
```

**Response shows:**
- Order processing stages
- Correlation tracking through multiple service calls
- Complete header context maintained throughout

## Configuration

The Azure Function is configured in `Program.cs` with:

```csharp
services.AddCorrelationIdWithHttpClient(options =>
{
    // Custom correlation ID header name
    options.CorrelationIdHeader = "X-Custom-Correlation-Id";
    
    // Additional headers to capture
    options.AdditionalHeaders.AddRange(new[]
    {
        "X-Event-Id",
        "X-User-Id", 
        "X-Request-Source",
        "X-Tenant-Id"
    });
    
    // Add captured headers to response
    options.AddAdditionalHeadersToResponse = true;
    
    // Auto-generate correlation ID if missing
    options.AutoGenerate = true;
    
    // Add correlation ID to response headers
    options.AddToResponseHeaders = true;
});
```

## Key Features Verified

✅ **Custom Header Names**: Uses `X-Custom-Correlation-Id` instead of default `X-Correlation-Id`
✅ **HTTP Propagation**: All headers (correlation ID + additional) flow to external APIs
✅ **Response Headers**: Captured headers are added to function responses
✅ **Structured Logging**: All log entries include correlation context automatically
✅ **Programmatic Control**: Headers can be set and updated during function execution
✅ **Multi-Stage Workflows**: Correlation context maintained across complex operations
✅ **Error Handling**: Correlation context preserved even when operations fail

## Architecture

```
HTTP Request
├── Custom Headers (X-Custom-Correlation-Id, X-User-Id, etc.)
├── Azure Function Processing
│   ├── Correlation ID Middleware (captures headers)
│   ├── Business Logic (headers available everywhere)
│   ├── HTTP Client Calls (headers auto-propagated)
│   └── Response Generation (headers added to response)
└── HTTP Response (with correlation headers)
```

## Logs

All function executions produce structured logs with correlation context:

```
[X-Custom-Correlation-Id: test-123] Processing weather request
[X-Custom-Correlation-Id: test-123] Calling external API for additional data
[X-Custom-Correlation-Id: test-123] External API call completed
```

## External Dependencies

Functions make HTTP calls to:
- **jsonplaceholder.typicode.com**: Mock data API
- **httpbin.org**: HTTP testing service that echoes headers

These demonstrate that correlation headers are automatically propagated to all external services.
