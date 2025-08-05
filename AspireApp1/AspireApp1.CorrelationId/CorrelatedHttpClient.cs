using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AspireApp1.CorrelationId;

/// <summary>
/// Service for making HTTP calls with automatic correlation ID propagation
/// </summary>
public interface ICorrelatedHttpClient
{
    /// <summary>
    /// Sends a GET request with correlation ID header
    /// </summary>
    Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a GET request and deserializes the response to the specified type
    /// </summary>
    Task<T?> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a POST request with JSON content and correlation ID header
    /// </summary>
    Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a POST request with JSON content and deserializes the response
    /// </summary>
    Task<TResponse?> PostAsJsonAsync<TRequest, TResponse>(string requestUri, TRequest content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a PUT request with JSON content and correlation ID header
    /// </summary>
    Task<HttpResponseMessage> PutAsJsonAsync<T>(string requestUri, T content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a DELETE request with correlation ID header
    /// </summary>
    Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the underlying HttpClient for advanced scenarios
    /// </summary>
    HttpClient HttpClient { get; }
}

/// <summary>
/// Implementation of correlated HTTP client
/// </summary>
public class CorrelatedHttpClient : ICorrelatedHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ICorrelationIdService _correlationIdService;
    private readonly ILogger<CorrelatedHttpClient> _logger;

    public CorrelatedHttpClient(HttpClient httpClient, ICorrelationIdService correlationIdService, ILogger<CorrelatedHttpClient> logger)
    {
        _httpClient = httpClient;
        _correlationIdService = correlationIdService;
        _logger = logger;
    }

    public HttpClient HttpClient => _httpClient;

    public async Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        LogRequest("GET", requestUri);
        var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        LogResponse("GET", requestUri, response);
        return response;
    }

    public async Task<T?> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        LogRequest("GET", requestUri);
        var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        LogResponse("GET", requestUri, response);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        return default;
    }

    public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T content, CancellationToken cancellationToken = default)
    {
        LogRequest("POST", requestUri);
        var json = JsonSerializer.Serialize(content);
        var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(requestUri, stringContent, cancellationToken);
        LogResponse("POST", requestUri, response);
        return response;
    }

    public async Task<TResponse?> PostAsJsonAsync<TRequest, TResponse>(string requestUri, TRequest content, CancellationToken cancellationToken = default)
    {
        LogRequest("POST", requestUri);
        var json = JsonSerializer.Serialize(content);
        var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(requestUri, stringContent, cancellationToken);
        LogResponse("POST", requestUri, response);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<TResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        return default;
    }

    public async Task<HttpResponseMessage> PutAsJsonAsync<T>(string requestUri, T content, CancellationToken cancellationToken = default)
    {
        LogRequest("PUT", requestUri);
        var json = JsonSerializer.Serialize(content);
        var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PutAsync(requestUri, stringContent, cancellationToken);
        LogResponse("PUT", requestUri, response);
        return response;
    }

    public async Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        LogRequest("DELETE", requestUri);
        var response = await _httpClient.DeleteAsync(requestUri, cancellationToken);
        LogResponse("DELETE", requestUri, response);
        return response;
    }

    private void LogRequest(string method, string requestUri)
    {
        _logger.LogInformation("Making {Method} request to {Uri}", method, requestUri);
    }

    private void LogResponse(string method, string requestUri, HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("{Method} request to {Uri} completed with {StatusCode}", 
                method, requestUri, (int)response.StatusCode);
        }
        else
        {
            _logger.LogWarning("{Method} request to {Uri} failed with {StatusCode}", 
                method, requestUri, (int)response.StatusCode);
        }
    }
}
