using NetCore.Donation.Application.Country.Update;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Tests.Country.Update;

[TestClass]
public class UpdateCountryCommandHandlerTest : BaseTest
{
    private readonly ICountryRepository countryRepository;
    private readonly IUnitOfWork unitOfWork;

    public UpdateCountryCommandHandlerTest()
    {
        var context = GetContext().Result;
        unitOfWork = context;
        countryRepository = new CountryRepository(context);
    }

    [TestMethod]
    [DataRow]
    public async Task UpdateCountryCommand_ShouldUpdateCountryAndCache()
    {
        // Arrange - Create a country first
        var country = Domain.Entities.Country.Create("Original Name", "001", "OR", "ORI");
        await countryRepository.AddAsync(country, default);
        await unitOfWork.SaveChangesAsync(default);

        var updateCommand = new UpdateCountryCommand(country.Id, "Updated Name", "002", "UP", "UPD");
        var handler = new UpdateCountriesCommandHandler(unitOfWork, countryRepository);

        // Act
        var result = await handler.Handle(updateCommand, default);

        // Assert
        Assert.IsTrue(result);

        var updatedCountry = await countryRepository.FindByIdAsync(country.Id);
        Assert.AreEqual("Updated Name", updatedCountry.Name);
        Assert.AreEqual("002", updatedCountry.CountryCode);
        Assert.AreEqual("UP", updatedCountry.Alpha2);
        Assert.AreEqual("UPD", updatedCountry.Alpha3);
    }

    [TestMethod]
    [DataRow]
    public async Task UpdateCountryCommand_WithNonExistentId_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateCommand = new UpdateCountryCommand(nonExistentId, "Test", "001", "TS", "TST");
        var handler = new UpdateCountriesCommandHandler(unitOfWork, countryRepository);

        // Act
        var result = await handler.Handle(updateCommand, default);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    [DataRow]
    public async Task UpdateCountriesCommand_ShouldUpdateMultipleCountriesAndCache()
    {
        // Arrange - Create multiple countries
        var country1 = Domain.Entities.Country.Create("Country 1", "001", "C1", "COU1");
        var country2 = Domain.Entities.Country.Create("Country 2", "002", "C2", "COU2");
        var country3 = Domain.Entities.Country.Create("Country 3", "003", "C3", "COU3");

        await countryRepository.AddAsync(new[] { country1, country2, country3 }, default);
        await unitOfWork.SaveChangesAsync(default);

        var updateCommands = new List<UpdateCountryCommand>
        {
            new UpdateCountryCommand(country1.Id, "Updated 1", "101", "U1", "UPD1"),
            new UpdateCountryCommand(country2.Id, "Updated 2", "102", "U2", "UPD2"),
            new UpdateCountryCommand(country3.Id, "Updated 3", "103", "U3", "UPD3"),
        };

        var command = new UpdateCountriesCommand(updateCommands);
        var handler = new UpdateCountriesCommandHandler(unitOfWork, countryRepository);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        Assert.IsTrue(result);

        var updated1 = await countryRepository.FindByIdAsync(country1.Id);
        Assert.AreEqual("Updated 1", updated1.Name);

        var updated2 = await countryRepository.FindByIdAsync(country2.Id);
        Assert.AreEqual("Updated 2", updated2.Name);

        var updated3 = await countryRepository.FindByIdAsync(country3.Id);
        Assert.AreEqual("Updated 3", updated3.Name);
    }
}