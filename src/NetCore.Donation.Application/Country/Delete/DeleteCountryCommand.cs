using MediatR;

namespace NetCore.Donation.Application.Country.Delete;

public sealed record DeleteCountryCommand(Guid Id) : IRequest<bool>;