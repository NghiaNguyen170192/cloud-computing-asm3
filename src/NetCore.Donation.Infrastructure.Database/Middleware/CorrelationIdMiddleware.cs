#nullable enable
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NetCore.Donation.Infrastructure.Database.Services;
using Serilog.Context;

namespace NetCore.Donation.Infrastructure.Database.Middleware;

/// <summary>
/// Middleware that extracts or creates a correlation ID from request headers.
/// The correlation ID is used to track requests across the entire system.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private const string CorrelationIdItemKey = "CorrelationId";

    private readonly RequestDelegate next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        this.next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract correlation ID from request header or generate new one
        var correlationId = ExtractOrCreateCorrelationId(context);

        // Store in HttpContext.Items for access throughout the request
        context.Items[CorrelationIdItemKey] = correlationId;
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        if (context.Request.Headers.TryGetValue(IdempotencyKeyAccessor.HeaderName, out var idempotencyHeader))
        {
            var idempotencyKey = idempotencyHeader.ToString();
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                context.Items[IdempotencyKeyAccessor.ItemKey] = idempotencyKey;
                context.Response.Headers[IdempotencyKeyAccessor.HeaderName] = idempotencyKey;
            }
        }

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Continue with pipeline
            await next(context);
        }
    }

    private static string ExtractOrCreateCorrelationId(HttpContext context)
    {
        // Try to extract from incoming request header
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationIdHeader))
        {
            var headerValue = correlationIdHeader.ToString();
            if (!string.IsNullOrWhiteSpace(headerValue))
            {
                return headerValue;
            }
        }

        // Generate new correlation ID if not provided
        return Guid.NewGuid().ToString("N");
    }
}

/// <summary>
/// Extension methods for registering correlation ID middleware.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adds the correlation ID middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        app.UseMiddleware<CorrelationIdMiddleware>();
        return app;
    }
}