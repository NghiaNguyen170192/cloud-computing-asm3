using MediatR;

namespace NetCore.Donation.Application.Country.Create;

public sealed record CreateCountriesCommand : IRequest<IEnumerable<Guid>>
{
    public required IEnumerable<CreateCountryCommand> Countries { get; init; }
}