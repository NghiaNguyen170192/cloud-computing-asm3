using MediatR;
using Microsoft.Extensions.Logging;
using NetCore.Donation.Domain.Events;

namespace NetCore.Donation.Application.Donation.Events;

public class ContactCreatedDomainEventHandler(ILogger<ContactCreatedDomainEventHandler> logger)
    : INotificationHandler<ContactCreatedDomainEvent>
{
    public Task Handle(ContactCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Contact created {ContactId} ({Email})",
            notification.ContactId,
            notification.Email);

        return Task.CompletedTask;
    }
}
