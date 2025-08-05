using System.Diagnostics;

namespace AspireApp1.CorrelationId;

/// <summary>
/// Helper class to automatically add correlation ID to method execution tracking
/// </summary>
public static class CorrelationIdHelper
{
    /// <summary>
    /// Executes a synchronous action with correlation ID context
    /// </summary>
    public static void ExecuteWithCorrelationId(ICorrelationIdService correlationIdService, Action action, string? methodName = null)
    {
        var correlationId = correlationIdService.CorrelationId;
        var caller = methodName ?? GetCallerMethodName();
        
        Activity.Current?.SetTag("CorrelationId", correlationId);
        Activity.Current?.SetTag("Method", caller);
        
        action();
    }

    /// <summary>
    /// Executes a synchronous function with correlation ID context
    /// </summary>
    public static T ExecuteWithCorrelationId<T>(ICorrelationIdService correlationIdService, Func<T> func, string? methodName = null)
    {
        var correlationId = correlationIdService.CorrelationId;
        var caller = methodName ?? GetCallerMethodName();
        
        Activity.Current?.SetTag("CorrelationId", correlationId);
        Activity.Current?.SetTag("Method", caller);
        
        return func();
    }

    /// <summary>
    /// Executes an asynchronous action with correlation ID context
    /// </summary>
    public static async Task ExecuteWithCorrelationIdAsync(ICorrelationIdService correlationIdService, Func<Task> action, string? methodName = null)
    {
        var correlationId = correlationIdService.CorrelationId;
        var caller = methodName ?? GetCallerMethodName();
        
        Activity.Current?.SetTag("CorrelationId", correlationId);
        Activity.Current?.SetTag("Method", caller);
        
        await action();
    }

    /// <summary>
    /// Executes an asynchronous function with correlation ID context
    /// </summary>
    public static async Task<T> ExecuteWithCorrelationIdAsync<T>(ICorrelationIdService correlationIdService, Func<Task<T>> func, string? methodName = null)
    {
        var correlationId = correlationIdService.CorrelationId;
        var caller = methodName ?? GetCallerMethodName();
        
        Activity.Current?.SetTag("CorrelationId", correlationId);
        Activity.Current?.SetTag("Method", caller);
        
        return await func();
    }

    private static string GetCallerMethodName()
    {
        var stackTrace = new StackTrace();
        var frame = stackTrace.GetFrame(2); // Skip current method and ExecuteWithCorrelationId
        return frame?.GetMethod()?.Name ?? "Unknown";
    }
}
