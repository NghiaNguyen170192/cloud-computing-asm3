using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;
using NetCore.Donation.Domain.Storage;

namespace NetCore.Donation.Application.Receipt.Update;

public class UpdateReceiptCommandHandler(
    IUnitOfWork unitOfWork,
    IReceiptRepository receiptRepository,
    IContactRepository contactRepository,
    ITransactionRepository transactionRepository,
    IPaymentMethodRepository paymentMethodRepository,
    IReceiptDocumentGenerator documentGenerator,
    IReceiptDocumentStorage documentStorage)
    : IRequestHandler<UpdateReceiptCommand, bool>
{
    public async Task<bool> Handle(UpdateReceiptCommand request, CancellationToken cancellationToken)
    {
        var receipt = await receiptRepository.FindByIdAsync(request.Id, cancellationToken);
        if (receipt is null)
        {
            return false;
        }

        Guid? paymentScheduleId = null;
        if (request.TransactionId is { } transactionId)
        {
            var transaction = await transactionRepository.FindByIdAsync(transactionId, cancellationToken);
            if (transaction is null)
            {
                throw new ArgumentException($"Transaction '{transactionId}' was not found.", nameof(request));
            }

            if (transaction.ContactId != receipt.ContactId)
            {
                throw new InvalidOperationException("The transaction does not belong to the contact.");
            }

            paymentScheduleId = transaction.PaymentScheduleId;
        }

        var transactionChanged = receipt.TransactionId != request.TransactionId;
        request.UpdateEntity(receipt, paymentScheduleId);

        if (transactionChanged)
        {
            await ReceiptDocumentService.AssignGeneratedDocumentAsync(
                receipt,
                contactRepository,
                transactionRepository,
                paymentMethodRepository,
                documentGenerator,
                documentStorage,
                cancellationToken);
            receipt.MarkGenerated();
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (transactionChanged && !string.IsNullOrWhiteSpace(receipt.DocumentObjectKey))
            {
                await documentStorage.DeleteAsync(receipt.DocumentObjectKey, cancellationToken);
            }

            throw;
        }

        return true;
    }
}
