using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCore.Donation.Domain.Storage;

namespace NetCore.Donation.Infrastructure.Storage;

public static class DependencyInjection
{
    public static IServiceCollection AddObjectStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ObjectStorageOptions>(configuration.GetSection(ObjectStorageOptions.SectionName));

        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            var options = configuration.GetSection(ObjectStorageOptions.SectionName).Get<ObjectStorageOptions>()
                ?? new ObjectStorageOptions();
            return S3ReceiptDocumentStorage.CreateClient(options);
        });

        services.AddSingleton<IReceiptDocumentStorage, S3ReceiptDocumentStorage>();
        services.AddSingleton<IReceiptDocumentGenerator, ReceiptPdfDocumentGenerator>();

        return services;
    }
}
