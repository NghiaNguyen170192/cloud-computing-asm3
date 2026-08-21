namespace NetCore.Donation.Domain.Storage;

public interface IReceiptDocumentGenerator
{
    Task<ReceiptDocumentContent> GenerateAsync(
        Guid receiptId,
        Guid contactId,
        Guid? transactionId,
        CancellationToken cancellationToken = default);
}
