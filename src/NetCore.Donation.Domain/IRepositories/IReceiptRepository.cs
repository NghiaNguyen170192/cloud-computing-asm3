using NetCore.Donation.Domain.Entities;

namespace NetCore.Donation.Domain.IRepositories;

public interface IReceiptRepository
{
    Task AddAsync(Receipt receipt, CancellationToken cancellationToken);

    Task<bool> IsExistAsync(Guid id, CancellationToken cancellationToken);

    Task<Receipt?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Receipt?> FindByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken);

    void Delete(Receipt receipt);

    IQueryable<Receipt> GetAll();
}