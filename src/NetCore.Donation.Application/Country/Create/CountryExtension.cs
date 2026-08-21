namespace NetCore.Donation.Application.Country.Create;

public static class CountryExtension
{
	public static Domain.Entities.Country ToDbEntity(this CreateCountryCommand request)
	{
		return Domain.Entities.Country.Create(
			request.Name,
			request.CountryCode,
			request.Alpha2,
			request.Alpha3);
	}
}