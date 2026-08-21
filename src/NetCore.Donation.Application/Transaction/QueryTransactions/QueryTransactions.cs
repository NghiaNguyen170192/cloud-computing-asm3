using MediatR;
using NetCore.Donation.Application.Transaction.DTOs;

namespace NetCore.Donation.Application.Transaction.QueryTransactions;

public sealed record QueryTransactions(Guid? ContactId = null, Guid? PaymentScheduleId = null)
    : IRequest<IQueryable<QueryTransactionDto>>;