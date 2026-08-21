using MediatR;
using NetCore.Donation.Application.Receipt.DTOs;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Application.Receipt.GetReceipt;

public class GetReceiptQueryHandler(IReceiptRepository receiptRepository)
    : IRequestHandler<GetReceiptQuery, QueryReceiptDto?>
{
    public Task<QueryReceiptDto?> Handle(GetReceiptQuery request, CancellationToken cancellationToken)
    {
        var receipt = receiptRepository.GetAll()
            .ToQueryDto()
            .FirstOrDefault(receipt => receipt.Id == request.Id);

        return Task.FromResult(receipt);
    }
}