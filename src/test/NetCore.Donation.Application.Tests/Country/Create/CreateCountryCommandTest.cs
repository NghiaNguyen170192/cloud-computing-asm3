using NetCore.Donation.Application.Country.Create;

namespace NetCore.Donation.Application.Tests.Country.Create;

[TestClass]
public class CreateCountryCommandTest
{
	[TestMethod]
	[DataRow("test 1", "999", "ab", "abc")]
	[DataRow("test 2", "998", "xy", "xyz")]
	[DataRow("test 3", "997", "jk", "jkl")]
	public void CommandShouldHaveSameInputData(string name, string countryCode, string alpha2, string alpha3)
	{
		// Arrange
		var dto1 = new CreateCountryCommand(name, countryCode, alpha2, alpha3);
		var dto2 = new CreateCountryCommand(name, countryCode, alpha2, alpha3);

		// Act 
		var result = dto1.Equals(dto2);

		// Assert
		Assert.IsTrue(result);

		Assert.AreEqual(dto1.Name, name);
		Assert.AreEqual(dto1.CountryCode, countryCode);
		Assert.AreEqual(dto1.Alpha2, alpha2);
		Assert.AreEqual(dto1.Alpha3, alpha3);

		Assert.AreEqual(dto2.Name, name);
		Assert.AreEqual(dto2.CountryCode, countryCode);
		Assert.AreEqual(dto2.Alpha2, alpha2);
		Assert.AreEqual(dto2.Alpha3, alpha3);
	}


	[TestMethod]
	[DataRow("test 1", "999", "ab", "abc")]
	[DataRow("test 2", "998", "xy", "xyz")]
	[DataRow("test 3", "997", "jk", "jkl")]
	public void ToCountryEntityExtensionShouldReturnSameCountryModel(string name, string countryCode, string alpha2, string alpha3)
	{
		// Arrange
		var command = new CreateCountryCommand(name, countryCode, alpha2, alpha3);
		var country = Domain.Entities.Country.Create(name, countryCode, alpha2, alpha3);

		// Act 
		var countryFromCommand = command.ToDbEntity();

		// Assert
		Assert.AreEqual(country.Name, countryFromCommand.Name);
		Assert.AreEqual(country.CountryCode, countryFromCommand.CountryCode);
		Assert.AreEqual(country.Alpha2, countryFromCommand.Alpha2);
		Assert.AreEqual(country.Alpha3, countryFromCommand.Alpha3);
	}
}
