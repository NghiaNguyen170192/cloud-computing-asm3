using MediatR;
using NetCore.Donation.Application.Transaction.DTOs;

namespace NetCore.Donation.Application.Transaction.GetTransaction;

public sealed record GetTransactionQuery(Guid Id) : IRequest<QueryTransactionDto?>;