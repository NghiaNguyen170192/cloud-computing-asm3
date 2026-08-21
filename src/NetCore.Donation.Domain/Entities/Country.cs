#nullable disable

using NetCore.Donation.Domain.Events;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Domain.Entities;

public class Country : Entity, IAggregateRoot
{
    public string Name { get; private set; }

    public string CountryCode { get; private set; }

    public string Alpha2 { get; private set; }

    public string Alpha3 { get; private set; }

    public static Country Create(string name, string countryCode, string alpha2, string alpha3)
    {
        var country = new Country(name, countryCode, alpha2, alpha3);

        country.AddDomainEvent(new CountryCreatedDomainEvent(country.Id, name));
        return country;
    }

    public void UpdateDetails(string name, string countryCode, string alpha2, string alpha3)
    {
        if (name != null)
        {
            Name = name;
        }

        if (countryCode != null)
        {
            CountryCode = countryCode;
        }

        if (alpha2 != null)
        {
            Alpha2 = alpha2;
        }

        if (alpha3 != null)
        {
            Alpha3 = alpha3;
        }
    }

    private Country(string name, string countryCode, string alpha2, string alpha3)
    {
        Name = name;
        CountryCode = countryCode;
        Alpha2 = alpha2;
        Alpha3 = alpha3;
    }
}