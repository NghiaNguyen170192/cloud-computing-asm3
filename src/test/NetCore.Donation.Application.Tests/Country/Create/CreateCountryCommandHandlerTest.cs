using NetCore.Donation.Application.Country.Create;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;
using NetCore.Donation.Infrastructure.Database.Repositories;

namespace NetCore.Donation.Application.Tests.Country.Create;

[TestClass]
public class CreateCountryCommandHandlerTest : BaseTest
{
    private readonly ICountryRepository countryRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly MockCacheRepository<Domain.Entities.Country> cacheRepository;

    public CreateCountryCommandHandlerTest()
    {
        var context = GetContext().Result;
        unitOfWork = context;
        countryRepository = new CountryRepository(context);
        cacheRepository = new MockCacheRepository<Domain.Entities.Country>();
    }

    [TestMethod]
    [DataRow]
    public async Task CreateCountriesCommand_ShouldReturnValidGuids()
    {
        // Arrange
        var command1 = new CreateCountryCommand("test 1", "999", "ab", "abc");
        var command2 = new CreateCountryCommand("test 2", "998", "xy", "xyz");
        var command3 = new CreateCountryCommand("test 3", "997", "jk", "jkl");

        var list = new List<CreateCountryCommand>
        {
            command1,
            command2,
            command3
        };

        var commands = new CreateCountriesCommand { Countries = list };

        var handler = new CreateCountriesCommandHandler(unitOfWork, countryRepository);

        // Act
        var ids = await handler.Handle(commands, default);

        // Assert
        Assert.IsNotNull(ids);
        Assert.AreEqual(ids.Count(), commands.Countries.Count());

        foreach (var id in ids)
        {
            Assert.AreNotEqual(id, Guid.Empty);
        }
    }

    [TestMethod]
    [DataRow]
    public async Task CreateCountryCommand_ShouldReturnValidGuid()
    {
        // Arrange
        var command = new CreateCountryCommand("test country", "100", "tc", "tst");
        var handler = new CreateCountriesCommandHandler(unitOfWork, countryRepository);

        // Act
        var id = await handler.Handle(command, default);

        // Assert
        Assert.AreNotEqual(id, Guid.Empty);

        var country = await countryRepository.FindByIdAsync(id);
        Assert.IsNotNull(country);
        Assert.AreEqual("test country", country.Name);
        Assert.AreEqual("100", country.CountryCode);
    }
}