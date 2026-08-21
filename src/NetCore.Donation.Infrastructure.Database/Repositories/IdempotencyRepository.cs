#nullable enable
using Microsoft.EntityFrameworkCore;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Infrastructure.Database.Repositories;

/// <summary>
/// Repository implementation for managing idempotency logs.
/// </summary>
public class IdempotencyRepository : IIdempotencyRepository
{
    private readonly ApplicationDatabaseContext context;

    public IdempotencyRepository(ApplicationDatabaseContext context)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IdempotencyLog?> GetByCorrelationIdAsync(string correlationId, string requestType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("Correlation ID cannot be null or whitespace.", nameof(correlationId));
        }

        if (string.IsNullOrWhiteSpace(requestType))
        {
            throw new ArgumentException("Request type cannot be null or whitespace.", nameof(requestType));
        }

        return await context.IdempotencyLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.CorrelationId == correlationId && x.RequestType == requestType && !x.IsExpired,
                cancellationToken);
    }

    public async Task AddAsync(IdempotencyLog idempotencyLog, CancellationToken cancellationToken = default)
    {
        if (idempotencyLog == null)
        {
            throw new ArgumentNullException(nameof(idempotencyLog));
        }

        await context.IdempotencyLogs.AddAsync(idempotencyLog, cancellationToken);
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expiredLogs = await context.IdempotencyLogs
            .Where(x => x.ExpiresAt <= DateTime.UtcNow || x.IsExpired)
            .ToListAsync(cancellationToken);

        if (expiredLogs.Count == 0)
        {
            return 0;
        }

        context.IdempotencyLogs.RemoveRange(expiredLogs);
        await context.SaveChangesAsync(cancellationToken);

        return expiredLogs.Count;
    }
}