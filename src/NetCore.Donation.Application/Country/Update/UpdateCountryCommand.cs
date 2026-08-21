using MediatR;

namespace NetCore.Donation.Application.Country.Update;

public sealed record UpdateCountryCommand(Guid Id, string Name, string CountryCode, string Alpha2, string Alpha3) : IRequest<bool>;