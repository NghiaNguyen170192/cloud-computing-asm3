using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NetCore.Donation.Domain.Storage;
using NetCore.Donation.Infrastructure.Database;
using NetCore.Donation.Infrastructure.Storage;

namespace NetCore.Donation.Api.Tests;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"InMemoryDbForTesting-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDatabaseContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDatabaseContext>>();
            services.RemoveAll<ApplicationDatabaseContext>();

            services.AddDbContext<ApplicationDatabaseContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName);
            });

            services.RemoveAll<IReceiptDocumentStorage>();
            services.RemoveAll<IReceiptDocumentGenerator>();
            services.RemoveAll<Amazon.S3.IAmazonS3>();
            services.AddSingleton<IReceiptDocumentStorage, InMemoryReceiptDocumentStorage>();
            services.AddSingleton<IReceiptDocumentGenerator, BlankReceiptDocumentGenerator>();
            services.RemoveAll<IHostedService>();
        });
    }
}
