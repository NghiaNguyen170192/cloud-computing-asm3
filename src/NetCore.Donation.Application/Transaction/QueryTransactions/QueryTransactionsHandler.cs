using MediatR;
using NetCore.Donation.Application.Transaction.DTOs;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Application.Transaction.QueryTransactions;

public class QueryTransactionsHandler(ITransactionRepository transactionRepository)
    : IRequestHandler<QueryTransactions, IQueryable<QueryTransactionDto>>
{
    public Task<IQueryable<QueryTransactionDto>> Handle(
        QueryTransactions request,
        CancellationToken cancellationToken)
    {
        var query = transactionRepository.GetAll().ToQueryDto();
        if (request.ContactId is { } contactId)
        {
            query = query.Where(transaction => transaction.ContactId == contactId);
        }

        if (request.PaymentScheduleId is { } paymentScheduleId)
        {
            query = query.Where(transaction => transaction.PaymentScheduleId == paymentScheduleId);
        }

        return Task.FromResult(query);
    }
}