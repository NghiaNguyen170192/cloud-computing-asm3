#nullable enable

using Microsoft.EntityFrameworkCore;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Infrastructure.Database.Repositories;

public class OutboxMessageRepository(ApplicationDatabaseContext applicationDatabaseContext) : IOutboxMessageRepository
{
    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int take, CancellationToken cancellationToken)
    {
        return await applicationDatabaseContext.OutboxMessages
            .Where(message => message.ProcessedAtUtc == null)
            .OrderBy(message => message.OccurredAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> FindByTraceAsync(
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var query = applicationDatabaseContext.OutboxMessages.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            query = query.Where(message => message.CorrelationId == correlationId);
        }

        return await query
            .OrderBy(message => message.OccurredAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> ListAsync(CancellationToken cancellationToken)
    {
        return await applicationDatabaseContext.OutboxMessages
            .AsNoTracking()
            .OrderBy(message => message.OccurredAtUtc)
            .ToListAsync(cancellationToken);
    }

    public IQueryable<OutboxMessage> GetAll()
    {
        return applicationDatabaseContext.OutboxMessages.AsNoTracking();
    }
}
