using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.Events;

namespace NetCore.Donation.Infrastructure.Database.Tests;

[TestClass]
public class ApplicationDatabaseContextTests
{
    [TestMethod]
    public async Task SaveChangesAsync_SetsAuditProperties()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ApplicationDatabaseContextTests>());
        var serviceProvider = services.BuildServiceProvider();
        var publisher = serviceProvider.GetRequiredService<IPublisher>();

        var context = new ApplicationDatabaseContext(options, publisher);
        var country = Country.Create("Test", "001", "TS", "TST");

        // Act
        context.Countries.Add(country);
        await context.SaveChangesAsync(CancellationToken.None);

        // Assert
        Assert.AreNotEqual(default, country.CreatedDate);
        Assert.AreNotEqual(default, country.ModifiedDate);
        Assert.AreEqual(DateTime.UtcNow.Date, country.CreatedDate.Date);
        Assert.AreEqual(DateTime.UtcNow.Date, country.ModifiedDate.Date);
    }

    [TestMethod]
    public async Task SaveChangesAsync_DispatchesDomainEvents()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var eventDispatched = false;
        TestEventHandler.SetHandler(() => eventDispatched = true);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ApplicationDatabaseContextTests>());
        services.AddScoped<INotificationHandler<CountryCreatedDomainEvent>, TestEventHandler>();
        var serviceProvider = services.BuildServiceProvider();
        var publisher = serviceProvider.GetRequiredService<IPublisher>();

        var context = new ApplicationDatabaseContext(options, publisher);
        await context.Database.EnsureCreatedAsync();
        var country = Country.Create("Test", "001", "TS", "TST");

        // Act
        context.Countries.Add(country);
        await context.SaveChangesAsync(CancellationToken.None);

        // Assert — domain events are captured in the outbox, not published inside SaveChanges
        Assert.IsFalse(eventDispatched);
        Assert.IsEmpty(country.DomainEvents);
        Assert.AreEqual(1, await context.OutboxMessages.CountAsync());
    }

    [TestMethod]
    public async Task SaveChangesAsync_WithoutDispatcher_DoesNotThrow()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDatabaseContext(options, null);
        var country = Country.Create("Test", "001", "TS", "TST");

        // Act
        context.Countries.Add(country);
        await context.SaveChangesAsync(CancellationToken.None);

        // Assert
        Assert.IsNotNull(country);
        Assert.AreNotEqual(Guid.Empty, country.Id);
    }

    [TestMethod]
    public void DbContext_HasCountriesDbSet()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Act
        var context = new ApplicationDatabaseContext(options, null);

        // Assert
        Assert.IsNotNull(context.Countries);
    }

    [TestMethod]
    public void DbContext_ImplementsIUnitOfWork()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Act
        var context = new ApplicationDatabaseContext(options, null);

        // Assert
        Assert.IsInstanceOfType(context, typeof(Domain.SharedKernel.IUnitOfWork));
    }

    // Test helpers
    private class TestEventHandler : INotificationHandler<CountryCreatedDomainEvent>
    {
        private static Action? _onHandle;

        public TestEventHandler()
        {
        }

        public static void SetHandler(Action onHandle)
        {
            _onHandle = onHandle;
        }

        public Task Handle(CountryCreatedDomainEvent notification, CancellationToken cancellationToken)
        {
            _onHandle?.Invoke();
            return Task.CompletedTask;
        }
    }
}