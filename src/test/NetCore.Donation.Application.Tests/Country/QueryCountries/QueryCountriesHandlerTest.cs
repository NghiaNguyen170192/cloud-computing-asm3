using NetCore.Donation.Application.Country.DTOs;
using NetCore.Donation.Application.Country.QueryCountries;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;
using NetCore.Donation.Infrastructure.Database.Repositories;

namespace NetCore.Donation.Application.Tests.Country.QueryCountries;

[TestClass]
public class QueryCountriesHandlerTest : BaseTest
{
    private readonly ICountryRepository countryRepository;
    private readonly IUnitOfWork unitOfWork;

    public QueryCountriesHandlerTest()
    {
        var context = GetContext().Result;
        unitOfWork = context;
        countryRepository = new CountryRepository(context);
    }

    [TestMethod]
    public async Task QueryCountriesShouldReturnIQueryable()
    {
        // Arrange
        var country1 = Domain.Entities.Country.Create("United States", "001", "US", "USA");
        var country2 = Domain.Entities.Country.Create("Canada", "002", "CA", "CAN");
        var country3 = Domain.Entities.Country.Create("Mexico", "003", "MX", "MEX");

        await countryRepository.AddAsync(country1, CancellationToken.None);
        await countryRepository.AddAsync(country2, CancellationToken.None);
        await countryRepository.AddAsync(country3, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(default);

        var query = new Application.Country.QueryCountries.QueryCountries();
        var handler = new QueryCountriesHandler(countryRepository);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IQueryable<QueryCountryDto>));

        var countries = result.ToList();
        Assert.AreEqual(3, countries.Count);

        var usCountry = countries.FirstOrDefault(c => c.Alpha2 == "US");
        Assert.IsNotNull(usCountry);
        Assert.AreEqual("United States", usCountry.Name);
        Assert.AreEqual("001", usCountry.CountryCode);
        Assert.AreEqual("USA", usCountry.Alpha3);
    }
}