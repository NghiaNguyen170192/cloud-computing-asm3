using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCore.Donation.Infrastructure.Database;

namespace NetCore.Donation.Application.Tests;

public class BaseTest
{
    protected static async Task<ApplicationDatabaseContext> GetContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDatabaseContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<BaseTest>());
        var serviceProvider = services.BuildServiceProvider();

        var publisher = serviceProvider.GetRequiredService<IPublisher>();
        var databaseContext = new ApplicationDatabaseContext(options, publisher);
        await databaseContext.Database.EnsureCreatedAsync();

        return databaseContext;
    }

    protected static IMediator GetMediator(IServiceProvider? serviceProvider = null)
    {
        if (serviceProvider != null)
        {
            return serviceProvider.GetRequiredService<IMediator>();
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<BaseTest>());
        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }
}