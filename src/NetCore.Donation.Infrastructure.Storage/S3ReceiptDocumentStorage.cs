using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using NetCore.Donation.Domain.Storage;

namespace NetCore.Donation.Infrastructure.Storage;

public sealed class S3ReceiptDocumentStorage : IReceiptDocumentStorage, IAsyncDisposable
{
    private readonly IAmazonS3 amazonS3;
    private readonly ObjectStorageOptions options;
    private readonly SemaphoreSlim bucketInitialization = new(1, 1);
    private bool bucketReady;

    public S3ReceiptDocumentStorage(IAmazonS3 amazonS3, IOptions<ObjectStorageOptions> options)
    {
        this.amazonS3 = amazonS3 ?? throw new ArgumentNullException(nameof(amazonS3));
        this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        ArgumentException.ThrowIfNullOrWhiteSpace(this.options.BucketName);
    }

    public async Task PutAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        await EnsureBucketAsync(cancellationToken);

        var request = new PutObjectRequest
        {
            BucketName = options.BucketName,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
        };

        await amazonS3.PutObjectAsync(request, cancellationToken);
    }

    public async Task<Stream?> GetAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);

        try
        {
            var response = await amazonS3.GetObjectAsync(
                options.BucketName,
                objectKey,
                cancellationToken);

            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);

        try
        {
            await amazonS3.DeleteObjectAsync(options.BucketName, objectKey, cancellationToken);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Idempotent delete: missing objects are ignored.
        }
    }

    public async Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);

        try
        {
            await amazonS3.GetObjectMetadataAsync(options.BucketName, objectKey, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public ValueTask DisposeAsync()
    {
        bucketInitialization.Dispose();
        amazonS3.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (bucketReady || !options.CreateBucketIfNotExists)
        {
            return;
        }

        await bucketInitialization.WaitAsync(cancellationToken);
        try
        {
            if (bucketReady)
            {
                return;
            }

            var exists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(
                amazonS3,
                options.BucketName);

            if (!exists)
            {
                await amazonS3.PutBucketAsync(options.BucketName, cancellationToken);
            }

            bucketReady = true;
        }
        finally
        {
            bucketInitialization.Release();
        }
    }

    public static IAmazonS3 CreateClient(ObjectStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var config = new AmazonS3Config
        {
            ForcePathStyle = options.ForcePathStyle,
        };

        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            config.ServiceURL = options.ServiceUrl;
            config.AuthenticationRegion = options.Region;
        }
        else
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
        }

        if (!string.IsNullOrWhiteSpace(options.AccessKey) && !string.IsNullOrWhiteSpace(options.SecretKey))
        {
            var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
            return new AmazonS3Client(credentials, config);
        }

        return new AmazonS3Client(config);
    }
}
