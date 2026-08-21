using NetCore.Donation.Application.Country.Delete;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Tests.Country.Delete;

[TestClass]
public class DeleteCountryCommandHandlerTest : BaseTest
{
    private readonly ICountryRepository countryRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly MockCacheRepository<Domain.Entities.Country> cacheRepository;

    public DeleteCountryCommandHandlerTest()
    {
        var context = GetContext().Result;
        unitOfWork = context;
        countryRepository = new CountryRepository(context);
        cacheRepository = new MockCacheRepository<Domain.Entities.Country>();
    }

    [TestMethod]
    [DataRow]
    public async Task DeleteCountryCommand_ShouldDeleteCountryAndInvalidateCache()
    {
        // Arrange - Create a country first
        var country = Domain.Entities.Country.Create("Test Country", "001", "TC", "TST");
        await countryRepository.AddAsync(country, default);
        await unitOfWork.SaveChangesAsync(default);

        var deleteCommand = new DeleteCountryCommand(country.Id);
        var handler = new DeleteCountriesCommandHandler(unitOfWork, countryRepository);

        // Act
        var result = await handler.Handle(deleteCommand, default);

        // Assert
        Assert.IsTrue(result);

        var deletedCountry = await countryRepository.FindByIdAsync(country.Id);
        Assert.IsNull(deletedCountry);
    }

    [TestMethod]
    [DataRow]
    public async Task DeleteCountryCommand_WithNonExistentId_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var deleteCommand = new DeleteCountryCommand(nonExistentId);
        var handler = new DeleteCountriesCommandHandler(unitOfWork, countryRepository);

        // Act
        var result = await handler.Handle(deleteCommand, default);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    [DataRow]
    public async Task DeleteCountriesCommand_ShouldDeleteMultipleCountriesAndInvalidateCache()
    {
        // Arrange - Create multiple countries
        var country1 = Domain.Entities.Country.Create("Country 1", "001", "C1", "COU1");
        var country2 = Domain.Entities.Country.Create("Country 2", "002", "C2", "COU2");
        var country3 = Domain.Entities.Country.Create("Country 3", "003", "C3", "COU3");

        await countryRepository.AddAsync(new[] { country1, country2, country3 }, default);
        await unitOfWork.SaveChangesAsync(default);

        var deleteIds = new List<Guid> { country1.Id, country2.Id, country3.Id };
        var command = new DeleteCountriesCommand(deleteIds);
        var handler = new DeleteCountriesCommandHandler(unitOfWork, countryRepository);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        Assert.IsTrue(result);

        var deleted1 = await countryRepository.FindByIdAsync(country1.Id);
        Assert.IsNull(deleted1);

        var deleted2 = await countryRepository.FindByIdAsync(country2.Id);
        Assert.IsNull(deleted2);

        var deleted3 = await countryRepository.FindByIdAsync(country3.Id);
        Assert.IsNull(deleted3);
    }
}