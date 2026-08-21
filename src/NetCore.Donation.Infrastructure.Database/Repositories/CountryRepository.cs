using Microsoft.EntityFrameworkCore;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Infrastructure.Database.Repositories;

public class CountryRepository(ApplicationDatabaseContext applicationDatabaseContext) : ICountryRepository
{
    public async Task AddAsync(Country country, CancellationToken cancellationToken)
    {
        var result = await applicationDatabaseContext.Countries.AddAsync(country, cancellationToken);
    }

    public async Task AddAsync(IEnumerable<Country> countries, CancellationToken cancellationToken)
    {
        await applicationDatabaseContext.Countries.AddRangeAsync(countries, cancellationToken);
    }

    public async Task<bool> IsExistAsync(string name, string countryCode, string alpha2, string alpha3)
    {
        var result = await applicationDatabaseContext.Countries
            .AsNoTracking()
            .AnyAsync(country =>
                country.Name == name && country.CountryCode == countryCode
                    && country.Alpha2 == alpha2 && country.Alpha3 == alpha3);

        return result;
    }

    public void Delete(Country country)
    {
        applicationDatabaseContext.Countries.Remove(country);
    }

    public void Delete(IEnumerable<Country> countries)
    {
        applicationDatabaseContext.Countries.RemoveRange(countries);
    }

    public async Task<Country> FindByIdAsync(Guid id)
    {
        return await applicationDatabaseContext.Countries.FindAsync(id);
    }

    public IQueryable<Country> GetAll()
    {
        return applicationDatabaseContext.Countries.AsNoTracking();
    }
}