using MediatR;
using Microsoft.Extensions.Logging;
using NetCore.Donation.Application.Receipt;
using NetCore.Donation.Domain.Events;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Application.Donation.Events;

public class ReceiptGeneratedDomainEventHandler(
    ILogger<ReceiptGeneratedDomainEventHandler> logger,
    IReceiptRepository receiptRepository,
    IContactRepository contactRepository,
    ITransactionRepository transactionRepository,
    IPaymentMethodRepository paymentMethodRepository)
    : INotificationHandler<ReceiptGeneratedDomainEvent>
{
    public async Task Handle(ReceiptGeneratedDomainEvent notification, CancellationToken cancellationToken)
    {
        var receipt = await receiptRepository.FindByIdAsync(notification.ReceiptId, cancellationToken);
        if (receipt is null)
        {
            logger.LogWarning(
                "Receipt {ReceiptId} was generated but could not be loaded for notification.",
                notification.ReceiptId);
            return;
        }

        var contact = await contactRepository.FindByIdAsync(receipt.ContactId, cancellationToken);
        if (contact is null)
        {
            logger.LogWarning(
                "Receipt {ReceiptId} has no contact {ContactId} for notification.",
                notification.ReceiptId,
                receipt.ContactId);
            return;
        }

        var fields = await ReceiptDocumentService.ResolveMergeFieldsAsync(
            receipt,
            contactRepository,
            transactionRepository,
            paymentMethodRepository,
            cancellationToken);
        var body = ReceiptMergeTemplate.Render(fields);

        if (!contact.DoNotEmail)
        {
            logger.LogInformation(
                "Email receipt {ReceiptIdentifier} to {Email}:{Body}",
                receipt.Identifier,
                contact.Email,
                $"{Environment.NewLine}{body}");
        }

        if (!contact.DoNotSms)
        {
            logger.LogInformation(
                "SMS receipt {ReceiptIdentifier} to {PhoneNumber}:{Body}",
                receipt.Identifier,
                contact.PhoneNumber,
                $"{Environment.NewLine}{body}");
        }
    }
}
