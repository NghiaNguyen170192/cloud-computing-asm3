using MediatR;
using NetCore.Donation.Application.Receipt.DTOs;

namespace NetCore.Donation.Application.Receipt.QueryReceipts;

public sealed record QueryReceipts(Guid? ContactId = null) : IRequest<IQueryable<QueryReceiptDto>>;