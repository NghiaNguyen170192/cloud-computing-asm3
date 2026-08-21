#nullable enable

using Microsoft.EntityFrameworkCore;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Infrastructure.Database.Repositories;

public class ContactRepository(ApplicationDatabaseContext applicationDatabaseContext) : IContactRepository
{
    public async Task AddAsync(Contact contact, CancellationToken cancellationToken)
    {
        await applicationDatabaseContext.Contacts.AddAsync(contact, cancellationToken);
    }

    public async Task<bool> IsExistAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await applicationDatabaseContext.Contacts
            .AsNoTracking()
            .AnyAsync(contact => contact.Id == id, cancellationToken);

        return result;
    }

    public async Task<Contact?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await applicationDatabaseContext.Contacts.FindAsync([id], cancellationToken);
    }

    public async Task<Contact?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await applicationDatabaseContext.Contacts
            .FirstOrDefaultAsync(contact => contact.Email.ToLower() == normalized, cancellationToken);
    }

    public void Delete(Contact contact)
    {
        applicationDatabaseContext.Contacts.Remove(contact);
    }

    public IQueryable<Contact> GetAll()
    {
        return applicationDatabaseContext.Contacts.AsNoTracking();
    }
}