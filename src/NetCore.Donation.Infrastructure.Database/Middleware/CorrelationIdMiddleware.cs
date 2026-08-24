#nullable enable
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
        var correlationId = ExtractOrCreateCorrelationId(context);

        context.Items[CorrelationIdItemKey] = correlationId;
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    private static string ExtractOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationIdHeader))
        {
            var headerValue = correlationIdHeader.ToString();
            if (!string.IsNullOrWhiteSpace(headerValue))
            {
                return headerValue;
            }
        }

        return Guid.NewGuid().ToString("N");
    }
}

/// <summary>
/// Extension methods for registering correlation ID middleware.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
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
