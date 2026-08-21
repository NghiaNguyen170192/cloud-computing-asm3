using NetCore.Donation.Domain.Entities;

namespace NetCore.Donation.Domain.IRepositories;

public interface IJournalRepository
{
    Task AddAsync(Journal journal, CancellationToken cancellationToken);

    Task<bool> IsExistAsync(Guid id, CancellationToken cancellationToken);

    Task<Journal?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Journal?> FindByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken);

    void Delete(Journal journal);

    IQueryable<Journal> GetAll();
}
