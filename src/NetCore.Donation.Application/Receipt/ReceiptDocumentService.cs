using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.Storage;

namespace NetCore.Donation.Application.Receipt;

public static class ReceiptDocumentService
{
    public const string PdfContentType = "application/pdf";

    public static string PdfFileName(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        return identifier.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            ? identifier
            : $"{identifier}.pdf";
    }

    public static string BuildObjectKey(string identifier)
    {
        return $"receipts/{PdfFileName(identifier)}";
    }

    public static async Task AssignGeneratedDocumentAsync(
        Domain.Entities.Receipt receipt,
        IContactRepository contactRepository,
        ITransactionRepository transactionRepository,
        IPaymentMethodRepository paymentMethodRepository,
        IReceiptDocumentGenerator documentGenerator,
        IReceiptDocumentStorage documentStorage,
        CancellationToken cancellationToken)
    {
        if (receipt.Id == Guid.Empty)
        {
            receipt.Id = Guid.NewGuid();
        }

        var fields = await ResolveMergeFieldsAsync(
            receipt,
            contactRepository,
            transactionRepository,
            paymentMethodRepository,
            cancellationToken);
        var body = ReceiptMergeTemplate.Render(fields);
        var previousObjectKey = receipt.DocumentObjectKey;
        var generated = await documentGenerator.GenerateAsync(fields.ReceiptNumber, body, cancellationToken);

        await using (generated.Content)
        {
            var objectKey = BuildObjectKey(fields.ReceiptNumber);
            await documentStorage.PutAsync(
                objectKey,
                generated.Content,
                generated.ContentType,
                cancellationToken);

            receipt.AssignDocument(
                objectKey,
                generated.FileName,
                generated.ContentType,
                generated.SizeBytes,
                DateTime.UtcNow);

            if (!string.IsNullOrWhiteSpace(previousObjectKey) &&
                !string.Equals(previousObjectKey, objectKey, StringComparison.Ordinal))
            {
                await documentStorage.DeleteAsync(previousObjectKey, cancellationToken);
            }
        }
    }

    public static async Task DeleteDocumentIfPresentAsync(
        Domain.Entities.Receipt receipt,
        IReceiptDocumentStorage documentStorage,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(receipt.DocumentObjectKey))
        {
            return;
        }

        await documentStorage.DeleteAsync(receipt.DocumentObjectKey, cancellationToken);
        receipt.ClearDocument();
    }

    public static async Task<ReceiptMergeFields> ResolveMergeFieldsAsync(
        Domain.Entities.Receipt receipt,
        IContactRepository contactRepository,
        ITransactionRepository transactionRepository,
        IPaymentMethodRepository paymentMethodRepository,
        CancellationToken cancellationToken)
    {
        var contact = await contactRepository.FindByIdAsync(receipt.ContactId, cancellationToken)
            ?? throw new InvalidOperationException($"Contact '{receipt.ContactId}' was not found.");

        var donorName = $"{contact.FirstName} {contact.LastName}".Trim();
        var donationAmount = ReceiptMergeTemplate.Unspecified;
        var donationDate = ReceiptMergeTemplate.FormatDate(DateOnly.FromDateTime(DateTime.UtcNow));
        var paymentMethod = ReceiptMergeTemplate.Unspecified;

        if (receipt.TransactionId is { } transactionId)
        {
            var transaction = await transactionRepository.FindByIdAsync(transactionId, cancellationToken);
            if (transaction is not null)
            {
                donationAmount = ReceiptMergeTemplate.FormatAmount(transaction.Amount);
                donationDate = ReceiptMergeTemplate.FormatDate(transaction.BookDate);
                var method = await paymentMethodRepository.FindByIdAsync(transaction.PaymentMethodId, cancellationToken);
                paymentMethod = string.IsNullOrWhiteSpace(method?.DisplayName)
                    ? transaction.PaymentType.ToString()
                    : method.DisplayName;
            }
        }

        return new ReceiptMergeFields(
            receipt.Identifier,
            donorName,
            donationAmount,
            donationDate,
            paymentMethod);
    }
}
