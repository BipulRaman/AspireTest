using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using AspireApp1.CorrelationId.AzureFunctions;

namespace AspireApp1.AzFunction.Functions;

/// <summary>
/// Order Function demonstrating complex workflows with correlation ID tracking
/// </summary>
public class OrderFunction : CorrelatedHttpFunction
{
    private readonly ICorrelatedHttpClient _httpClient;

    public OrderFunction(
        ILoggerFactory loggerFactory, 
        IEnhancedCorrelationIdService correlationIdService,
        IOptions<CorrelationIdOptions> options,
        ICorrelatedHttpClient httpClient)
        : base(loggerFactory.CreateLogger<OrderFunction>(), correlationIdService, options)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Process order with multiple service calls and correlation tracking
    /// </summary>
    [Function("ProcessOrder")]
    public async Task<HttpResponseData> ProcessOrder(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData req)
    {
        return await ExecuteWithCorrelationAsync(req, async () =>
        {
            Logger.LogInformation("Starting order processing");

            // Read order data
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            
            // Set initial processing headers
            CorrelationIdService.SetAdditionalHeaders(new Dictionary<string, string>
            {
                { "X-Operation", "order-processing" },
                { "X-Processing-Stage", "validation" },
                { "X-Order-Source", "api" }
            });

            // Step 1: Validate order
            Logger.LogInformation("Validating order data");
            await Task.Delay(100);
            
            // Update processing stage
            CorrelationIdService.SetAdditionalHeaders(new Dictionary<string, string>
            {
                { "X-Processing-Stage", "inventory-check" }
            });

            // Step 2: Check inventory
            Logger.LogInformation("Checking inventory availability");
            var inventoryResponse = await _httpClient.GetAsync("get"); // httpbin.org endpoint
            var inventoryAvailable = inventoryResponse.IsSuccessStatusCode;
            
            if (!inventoryAvailable)
            {
                Logger.LogWarning("Inventory check failed");
                return await CreateErrorResponseAsync(req, "Inventory unavailable", HttpStatusCode.BadRequest);
            }

            // Update processing stage
            CorrelationIdService.SetAdditionalHeaders(new Dictionary<string, string>
            {
                { "X-Processing-Stage", "payment-processing" }
            });

            // Step 3: Process payment
            Logger.LogInformation("Processing payment");
            var paymentData = new { amount = 99.99, currency = "USD" };
            var paymentResponse = await _httpClient.PostAsJsonAsync("post", paymentData);
            
            if (!paymentResponse.IsSuccessStatusCode)
            {
                Logger.LogError("Payment processing failed");
                return await CreateErrorResponseAsync(req, "Payment failed", HttpStatusCode.PaymentRequired);
            }

            // Update processing stage
            CorrelationIdService.SetAdditionalHeaders(new Dictionary<string, string>
            {
                { "X-Processing-Stage", "order-fulfillment" }
            });

            // Step 4: Create fulfillment order
            Logger.LogInformation("Creating fulfillment order");
            await Task.Delay(150);

            var orderId = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

            // Final processing stage
            CorrelationIdService.SetAdditionalHeaders(new Dictionary<string, string>
            {
                { "X-Processing-Stage", "completed" },
                { "X-Order-Id", orderId }
            });

            var orderResult = new
            {
                OrderId = orderId,
                Status = "Processed",
                CreatedAt = DateTime.UtcNow,
                Amount = 99.99,
                Currency = "USD",
                ProcessingStages = new[]
                {
                    "validation",
                    "inventory-check",
                    "payment-processing",
                    "order-fulfillment",
                    "completed"
                },
                CorrelationId = CorrelationIdService.CorrelationId,
                AllHeaders = CorrelationIdService.CapturedHeaders
            };

            Logger.LogInformation("Order processing completed successfully for order: {OrderId}", orderId);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(orderResult);
            return response;
        });
    }

    /// <summary>
    /// Get order status with correlation tracking
    /// </summary>
    [Function("GetOrderStatus")]
    public async Task<HttpResponseData> GetOrderStatus(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders/{orderId}/status")] HttpRequestData req,
        string orderId)
    {
        return await ExecuteWithCorrelationAsync(req, async () =>
        {
            Logger.LogInformation("Getting status for order: {OrderId}", orderId);

            // Set operation headers
            CorrelationIdService.SetAdditionalHeaders(new Dictionary<string, string>
            {
                { "X-Operation", "order-status-check" },
                { "X-Target-Order-Id", orderId }
            });

            // Simulate database lookup
            await Task.Delay(80);

            // Call external tracking service
            Logger.LogInformation("Checking external tracking system");
            var trackingResponse = await _httpClient.GetAsync($"status/200"); // httpbin.org endpoint
            
            var orderStatus = new
            {
                OrderId = orderId,
                Status = "In Transit",
                LastUpdated = DateTime.UtcNow.AddHours(-2),
                EstimatedDelivery = DateTime.UtcNow.AddDays(2),
                TrackingInfo = trackingResponse.IsSuccessStatusCode ? "Tracking available" : "Tracking unavailable",
                CorrelationId = CorrelationIdService.CorrelationId,
                RequestHeaders = CorrelationIdService.CapturedHeaders
            };

            Logger.LogInformation("Order status retrieved for order: {OrderId}", orderId);

            return await CreateJsonResponseAsync(req, orderStatus);
        });
    }

    /// <summary>
    /// Cancel order with rollback operations
    /// </summary>
    [Function("CancelOrder")]
    public async Task<HttpResponseData> CancelOrder(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "orders/{orderId}")] HttpRequestData req,
        string orderId)
    {
        return await ExecuteWithCorrelationAsync(req, async () =>
        {
            Logger.LogInformation("Starting order cancellation for order: {OrderId}", orderId);

            // Set cancellation headers
            CorrelationIdService.SetAdditionalHeaders(new Dictionary<string, string>
            {
                { "X-Operation", "order-cancellation" },
                { "X-Target-Order-Id", orderId },
                { "X-Cancellation-Stage", "initiated" }
            });

            // Step 1: Check if cancellation is allowed
            Logger.LogInformation("Checking if order can be cancelled");
            await Task.Delay(50);

            // Update cancellation stage
            CorrelationIdService.SetAdditionalHeaders(new Dictionary<string, string>
            {
                { "X-Cancellation-Stage", "payment-refund" }
            });

            // Step 2: Process refund
            Logger.LogInformation("Processing refund for order: {OrderId}", orderId);
            var refundResponse = await _httpClient.PostAsJsonAsync("post", new { orderId, action = "refund" });

            if (!refundResponse.IsSuccessStatusCode)
            {
                Logger.LogError("Refund processing failed for order: {OrderId}", orderId);
                return await CreateErrorResponseAsync(req, "Refund failed", HttpStatusCode.BadRequest);
            }

            // Update cancellation stage
            CorrelationIdService.SetAdditionalHeaders(new Dictionary<string, string>
            {
                { "X-Cancellation-Stage", "inventory-release" }
            });

            // Step 3: Release inventory
            Logger.LogInformation("Releasing inventory for order: {OrderId}", orderId);
            await Task.Delay(100);

            // Final stage
            CorrelationIdService.SetAdditionalHeaders(new Dictionary<string, string>
            {
                { "X-Cancellation-Stage", "completed" }
            });

            var cancellationResult = new
            {
                OrderId = orderId,
                Status = "Cancelled",
                CancelledAt = DateTime.UtcNow,
                RefundProcessed = true,
                InventoryReleased = true,
                CancellationStages = new[]
                {
                    "initiated",
                    "payment-refund",
                    "inventory-release",
                    "completed"
                },
                CorrelationId = CorrelationIdService.CorrelationId,
                ProcessingHeaders = CorrelationIdService.CapturedHeaders
            };

            Logger.LogInformation("Order cancellation completed for order: {OrderId}", orderId);

            return await CreateJsonResponseAsync(req, cancellationResult);
        });
    }

    private async Task<HttpResponseData> CreateErrorResponseAsync(HttpRequestData req, string message, HttpStatusCode statusCode)
    {
        var errorResponse = new
        {
            Error = message,
            Timestamp = DateTime.UtcNow,
            CorrelationId = CorrelationIdService.CorrelationId,
            Headers = CorrelationIdService.CapturedHeaders
        };

        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(errorResponse);
        return response;
    }
}
