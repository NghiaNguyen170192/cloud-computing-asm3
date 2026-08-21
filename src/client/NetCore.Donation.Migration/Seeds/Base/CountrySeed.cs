using CsvHelper;
using CsvHelper.Configuration;
using MediatR;
using NetCore.Donation.Application.Country.Create;
using NetCore.Donation.Application.Country.CsvMap;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Migration.Common.Interface;
using System.Globalization;

namespace NetCore.Donation.Migration.Seeds.Base;

public class CountrySeed(ISender dispatcher, ICountryRepository countryRepository) : IDataSeed
{
    private readonly string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Seeds");

    public IEnumerable<Type> Dependencies => new List<Type>();

    public async Task SeedAsync()
    {
        var input = Path.Combine(basePath, "countries.csv");
        var csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null,
            BadDataFound = null,
        };

        using var stream = new StreamReader(File.OpenRead(input));
        using var csv = new CsvReader(stream, csvConfiguration);
        csv.Context.RegisterClassMap<CountryCsvMap>();

        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            var command = GetCommand(csv);
            var exists = await countryRepository.IsExistAsync(
                command.Name,
                command.CountryCode,
                command.Alpha2,
                command.Alpha3);

            if (!exists)
            {
                await dispatcher.Send(command);
            }
        }
    }

    private CreateCountryCommand GetCommand(IReaderRow csv)
    {
        return new CreateCountryCommand(
            csv.GetField("name") ?? string.Empty,
            csv.GetField("country-code") ?? string.Empty,
            csv.GetField("alpha-2") ?? string.Empty,
            csv.GetField("alpha-3") ?? string.Empty);
    }
}