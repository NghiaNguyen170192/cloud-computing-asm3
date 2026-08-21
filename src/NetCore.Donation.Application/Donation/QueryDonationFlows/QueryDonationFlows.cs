using MediatR;
using NetCore.Donation.Application.Donation.DTOs;

namespace NetCore.Donation.Application.Donation.QueryDonationFlows;

public sealed record QueryDonationFlows : IRequest<IReadOnlyList<QueryDonationFlowDto>>;
