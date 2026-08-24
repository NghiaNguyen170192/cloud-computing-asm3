using NetCore.Donation.Domain.Entities;

namespace NetCore.Donation.Domain.IRepositories;

public interface IOutboxMessageRepository
{
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int take, CancellationToken cancellationToken);

    Task<IReadOnlyList<OutboxMessage>> FindByTraceAsync(
        string? correlationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OutboxMessage>> ListAsync(CancellationToken cancellationToken);

    IQueryable<OutboxMessage> GetAll();
}
