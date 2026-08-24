using MediatR;
using NetCore.Donation.Application.Receipt;
using NetCore.Donation.Domain.Enums;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;
using NetCore.Donation.Domain.Storage;

namespace NetCore.Donation.Application.Donation.GenerateDonationReceipt;

public class GenerateDonationReceiptCommandHandler(
    IUnitOfWork unitOfWork,
    IReceiptRepository receiptRepository,
    IContactRepository contactRepository,
    ITransactionRepository transactionRepository,
    IPaymentMethodRepository paymentMethodRepository,
    IReceiptDocumentGenerator documentGenerator,
    IReceiptDocumentStorage documentStorage)
    : IRequestHandler<GenerateDonationReceiptCommand, Guid>
{
    public async Task<Guid> Handle(GenerateDonationReceiptCommand request, CancellationToken cancellationToken)
    {
        var existing = await receiptRepository.FindByTransactionIdAsync(request.TransactionId, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        var transaction = await transactionRepository.FindByIdAsync(request.TransactionId, cancellationToken);
        if (transaction is null)
        {
            throw new ArgumentException($"Transaction '{request.TransactionId}' was not found.", nameof(request));
        }

        if (transaction.Status != TransactionStatus.Succeeded)
        {
            throw new InvalidOperationException(
                $"Receipts can only be generated for succeeded transactions. Status is '{transaction.Status}'.");
        }

        var receipt = Domain.Entities.Receipt.Create(
            transaction.ContactId,
            transaction.Id,
            transaction.PaymentScheduleId);

        try
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
            await receiptRepository.AddAsync(receipt, cancellationToken);
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
