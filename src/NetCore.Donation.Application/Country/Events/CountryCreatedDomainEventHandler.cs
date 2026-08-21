using MediatR;
using Microsoft.Extensions.Logging;
using NetCore.Donation.Domain.Events;

namespace NetCore.Donation.Application.Country.Events;

/// <summary>
/// Example domain event handler for CountryCreatedDomainEvent.
/// </summary>
public class CountryCreatedDomainEventHandler : INotificationHandler<CountryCreatedDomainEvent>
{
    private readonly ILogger<CountryCreatedDomainEventHandler> logger;

    public CountryCreatedDomainEventHandler(ILogger<CountryCreatedDomainEventHandler> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(CountryCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation($"Domain Event: Country '{notification.Name}' with ID '{notification.CountryId}' was created");

        // Add additional domain logic here (e.g., send notifications, update related aggregates, etc.)
        await Task.CompletedTask;
    }
}