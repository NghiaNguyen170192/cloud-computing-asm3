#nullable enable
using Microsoft.AspNetCore.Http;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Infrastructure.Database.Services;

/// <summary>
/// Implementation of ICorrelationIdAccessor that retrieves the correlation ID from the HTTP context.
/// </summary>
public class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private const string CorrelationIdItemKey = "CorrelationId";

    private readonly IHttpContextAccessor httpContextAccessor;

    public CorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public string CorrelationId
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return Guid.NewGuid().ToString("N");
            }

            // Try to get from Items (set by middleware)
            if (httpContext.Items.TryGetValue(CorrelationIdItemKey, out var correlationIdObj))
            {
                return correlationIdObj?.ToString() ?? Guid.NewGuid().ToString("N");
            }

            // Try to get from response headers (where middleware should have set it)
            if (httpContext.Response.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationIdHeader))
            {
                return correlationIdHeader.ToString();
            }

            // Generate new if not found
            var newCorrelationId = Guid.NewGuid().ToString("N");
            httpContext.Items[CorrelationIdItemKey] = newCorrelationId;
            return newCorrelationId;
        }
    }
}
