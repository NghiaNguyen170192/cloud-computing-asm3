using NetCore.Donation.Domain.Entities;

namespace NetCore.Donation.Domain.IRepositories;

public interface ICountryRepository
{
    Task AddAsync(Country country, CancellationToken cancellationToken);

    Task AddAsync(IEnumerable<Country> countries, CancellationToken cancellationToken);

    Task<bool> IsExistAsync(string name, string countryCode, string alpha2, string alpha3);

    void Delete(Country country);

    void Delete(IEnumerable<Country> countries);

    Task<Country> FindByIdAsync(Guid id);

    IQueryable<Country> GetAll();
}