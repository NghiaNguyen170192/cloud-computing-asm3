using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.Storage;

namespace NetCore.Donation.Application.Receipt;

public static class ReceiptDocumentService
{
    public static string BuildObjectKey(Guid receiptId)
    {
        return $"receipts/{receiptId:N}.pdf";
    }

    public static async Task AssignGeneratedDocumentAsync(
        Domain.Entities.Receipt receipt,
        IReceiptDocumentGenerator documentGenerator,
        IReceiptDocumentStorage documentStorage,
        CancellationToken cancellationToken)
    {
        if (receipt.Id == Guid.Empty)
        {
            receipt.Id = Guid.NewGuid();
        }

        var previousObjectKey = receipt.DocumentObjectKey;
        var generated = await documentGenerator.GenerateAsync(
            receipt.Id,
            receipt.ContactId,
            receipt.TransactionId,
            cancellationToken);

        await using (generated.Content)
        {
            var objectKey = BuildObjectKey(receipt.Id);
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
}
