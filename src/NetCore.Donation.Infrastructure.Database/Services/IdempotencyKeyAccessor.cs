#nullable enable
using Microsoft.AspNetCore.Http;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Infrastructure.Database.Services;

public class IdempotencyKeyAccessor(IHttpContextAccessor httpContextAccessor) : IIdempotencyKeyAccessor
{
    public const string HeaderName = "X-Idempotency-Key";
    public const string ItemKey = "IdempotencyKey";

    public string? IdempotencyKey
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return null;
            }

            if (httpContext.Items.TryGetValue(ItemKey, out var value) && value is string stored && !string.IsNullOrWhiteSpace(stored))
            {
                return stored;
            }

            if (httpContext.Request.Headers.TryGetValue(HeaderName, out var header))
            {
                var headerValue = header.ToString();
                if (!string.IsNullOrWhiteSpace(headerValue))
                {
                    return headerValue;
                }
            }

            return null;
        }
    }
}
