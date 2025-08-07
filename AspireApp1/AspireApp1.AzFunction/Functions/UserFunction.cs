using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using AspireApp1.CorrelationId.AzureFunctions;

namespace AspireApp1.AzFunction.Functions;

/// <summary>
/// User Function demonstrating correlation ID with user-specific operations
/// </summary>
public class UserFunction : CorrelatedHttpFunction
{
    private readonly HttpClient _externalApiClient;
    private readonly HttpClient _internalServiceClient;

    public UserFunction(
        ILoggerFactory loggerFactory, 
        IEnhancedCorrelationIdService correlationIdService,
        IOptions<CorrelationIdOptions> options,
        IHttpClientFactory httpClientFactory)
        : base(loggerFactory.CreateLogger<UserFunction>(), correlationIdService, options)
    {
        _externalApiClient = httpClientFactory.CreateClient(CorrelationIdHttpClientNames.ExternalApi);
        _internalServiceClient = httpClientFactory.CreateClient(CorrelationIdHttpClientNames.InternalService);
    }

    /// <summary>
    /// Get user profile with correlation ID tracking
    /// </summary>
    [Function("GetUserProfile")]
    public async Task<HttpResponseData> GetUserProfile(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users/{userId}")] HttpRequestData req,
        string userId)
    {
        return await ExecuteWithCorrelationAsync(req, async () =>
        {
            Logger.LogInformation("Getting user profile for user: {UserId}", userId);

            // Set user-specific headers
            CorrelationIdService.SetAdditionalHeaders(new Dictionary<string, string>
            {
                { "X-Target-User-Id", userId },
                { "X-Operation", "get-profile" }
            });

            // Simulate user lookup
            await Task.Delay(150);

            // Call external API for additional user data
            Logger.LogInformation("Fetching additional user data from external API");
            var externalResponse = await _externalApiClient.GetAsync($"users/{userId}");
            var externalData = externalResponse.IsSuccessStatusCode 
                ? await externalResponse.Content.ReadAsStringAsync() 
                : "External data unavailable";

            var userProfile = new
            {
                UserId = userId,
                Name = $"User {userId}",
                Email = $"user{userId}@example.com",
                CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 365)),
                ExternalData = externalData,
                CorrelationId = CorrelationIdService.CorrelationId,
                CapturedHeaders = CorrelationIdService.CapturedHeaders
            };

            Logger.LogInformation("User profile retrieved for user: {UserId}", userId);

            return await CreateJsonResponseAsync(req, userProfile);
        });
    }

    /// <summary>
    /// Create new user with multiple API calls
    /// </summary>
    [Function("CreateUser")]
    public async Task<HttpResponseData> CreateUser(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "users")] HttpRequestData req)
    {
        return await ExecuteWithCorrelationAsync(req, async () =>
        {
            Logger.LogInformation("Processing user creation request");

            // Read request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            
            // Set operation-specific headers
            CorrelationIdService.SetAdditionalHeaders(new Dictionary<string, string>
            {
                { "X-Operation", "create-user" },
                { "X-Processing-Stage", "validation" }
            });

            Logger.LogInformation("Validating user data");
            await Task.Delay(100); // Simulate validation

            // Update processing stage
            CorrelationIdService.SetAdditionalHeaders(new Dictionary<string, string>
            {
                { "X-Processing-Stage", "external-verification" }
            });

            // Call external service for verification
            Logger.LogInformation("Calling external verification service");
            var verificationResponse = await _externalApiClient.PostAsync("posts", 
                new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json"));

            // Update processing stage
            CorrelationIdService.SetAdditionalHeaders(new Dictionary<string, string>
            {
                { "X-Processing-Stage", "user-creation" }
            });

            Logger.LogInformation("Creating user in database");
            await Task.Delay(200); // Simulate database operation

            var newUserId = Random.Shared.Next(1000, 9999);
            var newUser = new
            {
                UserId = newUserId,
                Name = "New User",
                Email = "newuser@example.com",
                CreatedAt = DateTime.UtcNow,
                VerificationStatus = verificationResponse.IsSuccessStatusCode ? "Verified" : "Pending",
                CorrelationId = CorrelationIdService.CorrelationId,
                ProcessingHeaders = CorrelationIdService.CapturedHeaders
            };

            Logger.LogInformation("User created successfully with ID: {UserId}", newUserId);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(newUser);
            return response;
        });
    }

    /// <summary>
    /// Batch user operations demonstrating parallel processing with correlation context
    /// </summary>
    [Function("BatchProcessUsers")]
    public async Task<HttpResponseData> BatchProcessUsers(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "users/batch")] HttpRequestData req)
    {
        return await ExecuteWithCorrelationAsync(req, async () =>
        {
            Logger.LogInformation("Starting batch user processing");

            // Read user IDs from request
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var userIds = requestBody.Split(',').Select(id => id.Trim()).ToArray();

            Logger.LogInformation("Processing {UserCount} users in batch", userIds.Length);

            // Set batch operation headers
            CorrelationIdService.SetAdditionalHeaders(new Dictionary<string, string>
            {
                { "X-Operation", "batch-process" },
                { "X-Batch-Size", userIds.Length.ToString() },
                { "X-Processing-Mode", "parallel" }
            });

            // Process users in parallel - correlation context is automatically maintained
            var tasks = userIds.Select(async userId =>
            {
                Logger.LogInformation("Processing user: {UserId}", userId);
                
                // Simulate user processing
                await Task.Delay(Random.Shared.Next(100, 300));
                
                // Call external API for each user
                var response = await _externalApiClient.GetAsync($"users/{userId}");
                var userData = response.IsSuccessStatusCode 
                    ? await response.Content.ReadAsStringAsync() 
                    : $"Error processing user {userId}";

                Logger.LogInformation("Completed processing user: {UserId}", userId);

                return new { UserId = userId, Status = "Processed", Data = userData };
            });

            var results = await Task.WhenAll(tasks);

            var batchResult = new
            {
                BatchId = Guid.NewGuid().ToString(),
                ProcessedCount = results.Length,
                Users = results,
                CompletedAt = DateTime.UtcNow,
                CorrelationId = CorrelationIdService.CorrelationId,
                BatchHeaders = CorrelationIdService.CapturedHeaders
            };

            Logger.LogInformation("Batch processing completed for {ProcessedCount} users", results.Length);

            return await CreateJsonResponseAsync(req, batchResult);
        });
    }
}
