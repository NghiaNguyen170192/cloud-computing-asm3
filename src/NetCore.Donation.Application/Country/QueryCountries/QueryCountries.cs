using MediatR;
using NetCore.Donation.Application.Country.DTOs;

namespace NetCore.Donation.Application.Country.QueryCountries;

public sealed record QueryCountries : IRequest<IQueryable<QueryCountryDto>>;