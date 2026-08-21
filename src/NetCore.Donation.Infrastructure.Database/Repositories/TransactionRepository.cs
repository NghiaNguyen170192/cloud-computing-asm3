#nullable enable

using Microsoft.EntityFrameworkCore;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Infrastructure.Database.Repositories;

public class TransactionRepository(ApplicationDatabaseContext applicationDatabaseContext) : ITransactionRepository
{
    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        await applicationDatabaseContext.Transactions.AddAsync(transaction, cancellationToken);
    }

    public async Task<bool> IsExistAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await applicationDatabaseContext.Transactions
            .AsNoTracking()
            .AnyAsync(transaction => transaction.Id == id, cancellationToken);

        return result;
    }

    public async Task<Transaction?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await applicationDatabaseContext.Transactions.FindAsync([id], cancellationToken);
    }

    public async Task<Transaction?> FindByPaymentScheduleIdAsync(Guid paymentScheduleId, CancellationToken cancellationToken)
    {
        return await applicationDatabaseContext.Transactions
            .FirstOrDefaultAsync(transaction => transaction.PaymentScheduleId == paymentScheduleId, cancellationToken);
    }

    public void Delete(Transaction transaction)
    {
        applicationDatabaseContext.Transactions.Remove(transaction);
    }

    public IQueryable<Transaction> GetAll()
    {
        return applicationDatabaseContext.Transactions.AsNoTracking();
    }
}