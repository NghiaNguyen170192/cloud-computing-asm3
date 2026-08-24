namespace NetCore.Donation.Domain.Storage;

public interface IReceiptDocumentGenerator
{
    Task<ReceiptDocumentContent> GenerateAsync(
        string fileName,
        string body,
        CancellationToken cancellationToken = default);
}
