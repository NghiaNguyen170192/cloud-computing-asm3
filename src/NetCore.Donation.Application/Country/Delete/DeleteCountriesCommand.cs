using MediatR;

namespace NetCore.Donation.Application.Country.Delete;

public sealed record DeleteCountriesCommand(IEnumerable<Guid> Ids) : IRequest<bool>;