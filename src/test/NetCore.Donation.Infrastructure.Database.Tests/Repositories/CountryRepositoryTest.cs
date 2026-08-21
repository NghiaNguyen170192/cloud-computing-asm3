using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Infrastructure.Database.Repositories;

namespace NetCore.Donation.Infrastructure.Database.Tests.Repositories;

[TestClass]
public class CountryRepositoryTest : BaseTest
{
    [TestMethod]
    public async Task AddShouldReturnValidGuid()
    {
        // Arrange
        var unitOfWork = GetContext().Result;
        var countryRepository = new CountryRepository(unitOfWork);
        var country = Country.Create("test 1", "999", "ab", "abc");
        var beforeAddId = country.Id;

        // Act
        await countryRepository.AddAsync(country, default);
        var afterAddId = country.Id;

        // Assert
        Assert.AreEqual(Guid.Empty, beforeAddId);
        Assert.AreNotEqual(Guid.Empty, afterAddId);
    }

    [TestMethod]
    [DataRow("test 1", "999", "ab", "abc")]
    [DataRow("test 2", "998", "xy", "xyz")]
    [DataRow("test 3", "997", "jk", "jkl")]
    public async Task IsExistAsyncShouldReturnTrue(string name, string countryCode, string alpha2, string alpha3)
    {
        // Arrange
        var unitOfWork = GetContext().Result;
        var countryRepository = new CountryRepository(unitOfWork);
        var country = Country.Create(name, countryCode, alpha2, alpha3);

        Assert.AreEqual(0, unitOfWork.Countries.Count());

        await countryRepository.AddAsync(country, default);
        await unitOfWork.SaveChangesAsync(default);

        // Act
        var result = await countryRepository.IsExistAsync(name, countryCode, alpha2, alpha3);

        // Assert
        Assert.AreNotEqual(0, unitOfWork.Countries.Count());
        Assert.IsTrue(result);
    }

    [TestMethod]
    [DataRow("test 1", "999", "ab", "abc")]
    [DataRow("test 2", "998", "xy", "xyz")]
    [DataRow("test 3", "997", "jk", "jkl")]
    public async Task IsExistAsyncShouldReturnFalse(string name, string countryCode, string alpha2, string alpha3)
    {
        // Arrange
        var unitOfWork = GetContext().Result;
        var countryRepository = new CountryRepository(unitOfWork);
        var country = Country.Create(name, countryCode, alpha2, alpha3);

        await countryRepository.AddAsync(country, default);
        await unitOfWork.SaveChangesAsync(default);

        // Act
        var results = new List<bool>
        {
            await countryRepository.IsExistAsync("", countryCode, alpha2, alpha3),
            await countryRepository.IsExistAsync(name, "", alpha2, alpha3),
            await countryRepository.IsExistAsync(name, countryCode, "", alpha3),
            await countryRepository.IsExistAsync(name, countryCode, alpha2, ""),
            await countryRepository.IsExistAsync("", "", alpha2, alpha3),
            await countryRepository.IsExistAsync(name, "", "", alpha3),
            await countryRepository.IsExistAsync(name, countryCode, "", ""),
            await countryRepository.IsExistAsync("", "", "", alpha3),
            await countryRepository.IsExistAsync(name, "", "", ""),
            await countryRepository.IsExistAsync("", "", "", ""),
        };

        // Assert
        foreach (var result in results)
        {
            Assert.IsFalse(result);
        }
    }

    [TestMethod]
    public async Task DeleteShouldRemoveExistingEntity()
    {
        // Arrange
        var unitOfWork = GetContext().Result;
        var countryRepository = new CountryRepository(unitOfWork);
        var country = Country.Create("test 1", "999", "ab", "abc");

        await countryRepository.AddAsync(country, default);
        await unitOfWork.SaveChangesAsync(default);

        Assert.AreNotEqual(0, unitOfWork.Countries.Count());

        // Act
        countryRepository.Delete(country);
        await unitOfWork.SaveChangesAsync(default);

        // Assert
        Assert.AreEqual(0, unitOfWork.Countries.Count());
    }

    [TestMethod]
    public async Task DeleteShouldThrowExceptionForNotFoundEntity()
    {
        // Arrange
        var unitOfWork = GetContext().Result;
        var countryRepository = new CountryRepository(unitOfWork);

        var country = Country.Create("test 1", "999", "ab", "abc");
        var countryToBeDeleted = Country.Create("test 2", "998", "xy", "xyz");

        // Act
        await countryRepository.AddAsync(country, default);
        await unitOfWork.SaveChangesAsync(default);

        // Assert
        // before save
        Assert.AreEqual(1, unitOfWork.Countries.Count());

        countryRepository.Delete(countryToBeDeleted);

        await Assert.ThrowsExactlyAsync<DbUpdateConcurrencyException>(async () =>
        {
            await unitOfWork.SaveChangesAsync(default);
        });

        await Assert.ThrowsExactlyAsync<DbUpdateConcurrencyException>(async () =>
        {
            await unitOfWork.SaveChangesAsync(default);
        });

        // after save
        Assert.AreEqual(1, unitOfWork.Countries.Count());
    }

    [TestMethod]
    public async Task FindByIdAsyncShouldReturnNotNullCountry()
    {
        // Arrange
        var unitOfWork = GetContext().Result;
        var countryRepository = new CountryRepository(unitOfWork);
        var country = Country.Create("test 1", "999", "ab", "abc");

        await countryRepository.AddAsync(country, default);
        await unitOfWork.SaveChangesAsync(default);

        // Act 
        var result = await countryRepository.FindByIdAsync(country.Id);

        // Assert
        Assert.AreNotEqual(0, unitOfWork.Countries.Count());
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task FindByIdAsyncShouldReturnNull()
    {
        // Arrange
        var unitOfWork = GetContext().Result;
        var countryRepository = new CountryRepository(unitOfWork);

        // Act
        var result = await countryRepository.FindByIdAsync(Guid.NewGuid());

        // Assert
        Assert.AreEqual(0, unitOfWork.Countries.Count());
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetQueryableCountriesShouldReturnIQueryable()
    {
        // Arrange
        var unitOfWork = GetContext().Result;
        var countryRepository = new CountryRepository(unitOfWork);
        var country1 = Country.Create("test 1", "999", "aa", "abc");
        var country2 = Country.Create("test 2", "998", "ab", "aab");
        var country3 = Country.Create("test 3", "997", "ac", "aac");

        await countryRepository.AddAsync(country1, default);
        await countryRepository.AddAsync(country2, default);
        await countryRepository.AddAsync(country3, default);
        await unitOfWork.SaveChangesAsync(default);

        // Act
        var query = countryRepository.GetAll();
        var result1 = await query.Where(x => x.Name.StartsWith("test")).ToListAsync(TestContext.CancellationToken);
        var result2 = await query.Where(x => x.Alpha3.StartsWith("aa")).ToListAsync(TestContext.CancellationToken);

        // Assert
        Assert.AreNotEqual(0, result1.Count);
        Assert.HasCount(3, result1);

        Assert.AreNotEqual(0, result2.Count);
        Assert.HasCount(2, result2);
    }

    public TestContext TestContext { get; set; }
}