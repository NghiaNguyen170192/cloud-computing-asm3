using System.Collections.Concurrent;
using NetCore.Donation.Domain.Storage;

namespace NetCore.Donation.Infrastructure.Storage;

public sealed class InMemoryReceiptDocumentStorage : IReceiptDocumentStorage
{
    private readonly ConcurrentDictionary<string, byte[]> objects = new(StringComparer.Ordinal);

    public Task PutAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        ArgumentNullException.ThrowIfNull(content);

        using var memoryStream = new MemoryStream();
        content.CopyTo(memoryStream);
        objects[objectKey] = memoryStream.ToArray();
        return Task.CompletedTask;
    }

    public Task<Stream?> GetAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        if (!objects.TryGetValue(objectKey, out var bytes))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(new MemoryStream(bytes, writable: false));
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        objects.TryRemove(objectKey, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(objects.ContainsKey(objectKey));
    }
}
