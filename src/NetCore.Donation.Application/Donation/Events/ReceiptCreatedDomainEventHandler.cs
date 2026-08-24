using MediatR;
using Microsoft.Extensions.Logging;
using NetCore.Donation.Domain.Events;

namespace NetCore.Donation.Application.Donation.Events;

public class ReceiptCreatedDomainEventHandler(ILogger<ReceiptCreatedDomainEventHandler> logger)
    : INotificationHandler<ReceiptCreatedDomainEvent>
{
    public Task Handle(ReceiptCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Receipt created {ReceiptId} for contact {ContactId}",
            notification.ReceiptId,
            notification.ContactId);

        return Task.CompletedTask;
    }
}
