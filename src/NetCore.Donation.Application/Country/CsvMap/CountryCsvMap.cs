using CsvHelper.Configuration;
using System.Globalization;

namespace NetCore.Donation.Application.Country.CsvMap;

public sealed class CountryCsvMap : ClassMap<Domain.Entities.Country>
{
    public CountryCsvMap()
    {
        AutoMap(CultureInfo.InvariantCulture);
        Map(m => m.Name).Name("name");
        Map(m => m.CountryCode).Name("country-code");
        Map(m => m.Alpha2).Name("alpha-2");
        Map(m => m.Alpha3).Name("alpha-3");
    }
}