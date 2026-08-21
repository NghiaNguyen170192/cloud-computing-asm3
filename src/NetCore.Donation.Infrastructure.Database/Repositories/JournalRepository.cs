#nullable enable

using Microsoft.EntityFrameworkCore;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Infrastructure.Database.Repositories;

public class JournalRepository(ApplicationDatabaseContext applicationDatabaseContext) : IJournalRepository
{
    public async Task AddAsync(Journal journal, CancellationToken cancellationToken)
    {
        await applicationDatabaseContext.Journals.AddAsync(journal, cancellationToken);
    }

    public async Task<bool> IsExistAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await applicationDatabaseContext.Journals
            .AsNoTracking()
            .AnyAsync(journal => journal.Id == id, cancellationToken);

        return result;
    }

    public async Task<Journal?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await applicationDatabaseContext.Journals.FindAsync([id], cancellationToken);
    }

    public async Task<Journal?> FindByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        return await applicationDatabaseContext.Journals
            .AsNoTracking()
            .FirstOrDefaultAsync(journal => journal.TransactionId == transactionId, cancellationToken);
    }

    public void Delete(Journal journal)
    {
        applicationDatabaseContext.Journals.Remove(journal);
    }

    public IQueryable<Journal> GetAll()
    {
        return applicationDatabaseContext.Journals.AsNoTracking();
    }
}
