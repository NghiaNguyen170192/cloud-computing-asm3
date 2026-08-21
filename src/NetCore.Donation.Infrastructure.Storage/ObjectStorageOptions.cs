namespace NetCore.Donation.Infrastructure.Storage;

public sealed class ObjectStorageOptions
{
    public const string SectionName = "ObjectStorage";

    public string BucketName { get; set; } = "receipts";

    public string? ServiceUrl { get; set; }

    public string Region { get; set; } = "us-east-1";

    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    public bool ForcePathStyle { get; set; }

    public bool CreateBucketIfNotExists { get; set; }
}
