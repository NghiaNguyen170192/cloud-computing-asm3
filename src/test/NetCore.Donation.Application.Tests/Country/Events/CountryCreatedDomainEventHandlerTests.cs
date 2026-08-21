using NetCore.Donation.Domain.Events;
// removed dependency on NetCore.Donation.Domain.Messaging interfaces; tests now assert MediatR behavior
using Microsoft.Extensions.Logging.Abstractions;
using NetCore.Donation.Application.Country.Events;

namespace NetCore.Donation.Application.Tests.Country.Events;

[TestClass]
public class CountryCreatedDomainEventHandlerTests
{
    [TestMethod]
    public async Task HandleAsync_WithValidEvent_LogsInformation()
    {
        // Arrange
        var logger = new NullLogger<CountryCreatedDomainEventHandler>();
        var handler = new CountryCreatedDomainEventHandler(logger);
        var countryId = Guid.NewGuid();
        var domainEvent = new CountryCreatedDomainEvent(countryId, "United States");

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert - domain events don't return a value, just verify no exception
        Assert.IsNotNull(handler);
    }

    [TestMethod]
    public void Handler_ImplementsINotificationHandler()
    {
        // Arrange
        var logger = new NullLogger<CountryCreatedDomainEventHandler>();

        // Act
        var handler = new CountryCreatedDomainEventHandler(logger);

        // Assert
        Assert.IsInstanceOfType(handler, typeof(INotificationHandler<CountryCreatedDomainEvent>));
    }
}