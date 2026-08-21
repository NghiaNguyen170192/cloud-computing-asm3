namespace NetCore.Donation.Domain.Storage;

public interface IReceiptDocumentStorage
{
    Task PutAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream?> GetAsync(string objectKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default);
}
