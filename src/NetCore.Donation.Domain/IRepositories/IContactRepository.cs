using NetCore.Donation.Domain.Entities;

namespace NetCore.Donation.Domain.IRepositories;

public interface IContactRepository
{
    Task AddAsync(Contact contact, CancellationToken cancellationToken);

    Task<bool> IsExistAsync(Guid id, CancellationToken cancellationToken);

    Task<Contact?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Contact?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    void Delete(Contact contact);

    IQueryable<Contact> GetAll();
}