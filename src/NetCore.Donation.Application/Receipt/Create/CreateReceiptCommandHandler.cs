using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;
using NetCore.Donation.Domain.Storage;

namespace NetCore.Donation.Application.Receipt.Create;

public class CreateReceiptCommandHandler(
    IUnitOfWork unitOfWork,
    IReceiptRepository receiptRepository,
    IContactRepository contactRepository,
    ITransactionRepository transactionRepository,
    IReceiptDocumentGenerator documentGenerator,
    IReceiptDocumentStorage documentStorage)
    : IRequestHandler<CreateReceiptCommand, Guid>
{
    public async Task<Guid> Handle(CreateReceiptCommand request, CancellationToken cancellationToken)
    {
        if (!await contactRepository.IsExistAsync(request.ContactId, cancellationToken))
        {
            throw new ArgumentException($"Contact '{request.ContactId}' was not found.", nameof(request));
        }

        Guid? paymentScheduleId = null;
        if (request.TransactionId is { } transactionId)
        {
            var transaction = await transactionRepository.FindByIdAsync(transactionId, cancellationToken);
            if (transaction is null)
            {
                throw new ArgumentException($"Transaction '{transactionId}' was not found.", nameof(request));
            }

            if (transaction.ContactId != request.ContactId)
            {
                throw new InvalidOperationException("The transaction does not belong to the contact.");
            }

            paymentScheduleId = transaction.PaymentScheduleId;
        }

        var receipt = request.ToDbEntity(paymentScheduleId);
        await ReceiptDocumentService.AssignGeneratedDocumentAsync(
            receipt,
            documentGenerator,
            documentStorage,
            cancellationToken);

        await receiptRepository.AddAsync(receipt, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(receipt.DocumentObjectKey))
            {
                await documentStorage.DeleteAsync(receipt.DocumentObjectKey, cancellationToken);
            }

            throw;
        }

        return receipt.Id;
    }
}
