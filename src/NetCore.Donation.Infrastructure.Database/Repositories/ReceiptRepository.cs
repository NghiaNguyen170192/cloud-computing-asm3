#nullable enable

using Microsoft.EntityFrameworkCore;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Infrastructure.Database.Repositories;

public class ReceiptRepository(ApplicationDatabaseContext applicationDatabaseContext) : IReceiptRepository
{
    public async Task AddAsync(Receipt receipt, CancellationToken cancellationToken)
    {
        await applicationDatabaseContext.Receipts.AddAsync(receipt, cancellationToken);
    }

    public async Task<bool> IsExistAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await applicationDatabaseContext.Receipts
            .AsNoTracking()
            .AnyAsync(receipt => receipt.Id == id, cancellationToken);

        return result;
    }

    public async Task<Receipt?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await applicationDatabaseContext.Receipts.FindAsync([id], cancellationToken);
    }

    public async Task<Receipt?> FindByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        return await applicationDatabaseContext.Receipts
            .AsNoTracking()
            .FirstOrDefaultAsync(receipt => receipt.TransactionId == transactionId, cancellationToken);
    }

    public void Delete(Receipt receipt)
    {
        applicationDatabaseContext.Receipts.Remove(receipt);
    }

    public IQueryable<Receipt> GetAll()
    {
        return applicationDatabaseContext.Receipts.AsNoTracking();
    }
}