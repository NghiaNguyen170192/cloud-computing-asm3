using MediatR;
using NetCore.Donation.Application.Receipt.DTOs;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Application.Receipt.QueryReceipts;

public class QueryReceiptsHandler(IReceiptRepository receiptRepository)
    : IRequestHandler<QueryReceipts, IQueryable<QueryReceiptDto>>
{
    public Task<IQueryable<QueryReceiptDto>> Handle(QueryReceipts request, CancellationToken cancellationToken)
    {
        var query = receiptRepository.GetAll().ToQueryDto();
        if (request.ContactId is { } contactId)
        {
            query = query.Where(receipt => receipt.ContactId == contactId);
        }

        return Task.FromResult(query);
    }
}