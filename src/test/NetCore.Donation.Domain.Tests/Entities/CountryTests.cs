using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Domain.Tests.Entities;

[TestClass]
public class CountryTests
{
    [TestMethod]
    public void Constructor_WithValidParameters_CreatesCountry()
    {
        // Arrange
        var name = "United States";
        var countryCode = "840";
        var alpha2 = "US";
        var alpha3 = "USA";

        // Act
        var country = Country.Create(name, countryCode, alpha2, alpha3);

        // Assert
        Assert.AreEqual(name, country.Name);
        Assert.AreEqual(countryCode, country.CountryCode);
        Assert.AreEqual(alpha2, country.Alpha2);
        Assert.AreEqual(alpha3, country.Alpha3);
        Assert.AreEqual(Guid.Empty, country.Id); // ID is empty before persistence
    }

    [TestMethod]
    public void Country_IsEntity_ImplementsIAggregateRoot()
    {
        // Arrange & Act
        var country = Country.Create("Test", "001", "TS", "TST");

        // Assert
        Assert.IsInstanceOfType(country, typeof(IAggregateRoot));
        Assert.IsInstanceOfType(country, typeof(Entity));
    }

    [TestMethod]
    public void TwoCountries_WithSameId_AreEqual()
    {
        // Arrange
        var testId = Guid.NewGuid();
        var country1 = Country.Create("Test1", "001", "T1", "TS1");
        var country2 = Country.Create("Test2", "002", "T2", "TS2");
        country1.GetType().GetProperty("Id")!.SetValue(country1, testId);
        country2.GetType().GetProperty("Id")!.SetValue(country2, testId);

        // Act & Assert
        Assert.IsTrue(country1.Equals(country2));
        Assert.IsTrue(country1 == country2);
    }

    [TestMethod]
    public void TwoCountries_WithDifferentId_AreNotEqual()
    {
        // Arrange
        var country1 = Country.Create("Test1", "001", "T1", "TS1");
        var country2 = Country.Create("Test2", "002", "T2", "TS2");

        // Act & Assert
        Assert.IsFalse(country1.Equals(country2));
        Assert.IsTrue(country1 != country2);
    }
}