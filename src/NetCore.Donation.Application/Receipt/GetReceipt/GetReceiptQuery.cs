using MediatR;
using NetCore.Donation.Application.Receipt.DTOs;

namespace NetCore.Donation.Application.Receipt.GetReceipt;

public sealed record GetReceiptQuery(Guid Id) : IRequest<QueryReceiptDto?>;