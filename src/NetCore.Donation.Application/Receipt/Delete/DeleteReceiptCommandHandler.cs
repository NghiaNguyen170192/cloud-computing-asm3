using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;
using NetCore.Donation.Domain.Storage;

namespace NetCore.Donation.Application.Receipt.Delete;

public class DeleteReceiptCommandHandler(
    IUnitOfWork unitOfWork,
    IReceiptRepository receiptRepository,
    IReceiptDocumentStorage documentStorage)
    : IRequestHandler<DeleteReceiptCommand, bool>
{
    public async Task<bool> Handle(DeleteReceiptCommand request, CancellationToken cancellationToken)
    {
        var receipt = await receiptRepository.FindByIdAsync(request.Id, cancellationToken);
        if (receipt is null)
        {
            return false;
        }

        var objectKey = receipt.DocumentObjectKey;
        receiptRepository.Delete(receipt);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(objectKey))
        {
            await documentStorage.DeleteAsync(objectKey, cancellationToken);
        }

        return true;
    }
}
