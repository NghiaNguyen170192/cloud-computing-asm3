using MediatR;
using NetCore.Donation.Application.Transaction.DTOs;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Application.Transaction.GetTransaction;

public class GetTransactionQueryHandler(ITransactionRepository transactionRepository)
    : IRequestHandler<GetTransactionQuery, QueryTransactionDto?>
{
    public Task<QueryTransactionDto?> Handle(GetTransactionQuery request, CancellationToken cancellationToken)
    {
        var transaction = transactionRepository.GetAll()
            .ToQueryDto()
            .FirstOrDefault(transaction => transaction.Id == request.Id);

        return Task.FromResult(transaction);
    }
}