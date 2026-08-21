using MediatR;
using Microsoft.Extensions.Logging;
using NetCore.Donation.Domain.Events;

namespace NetCore.Donation.Application.Donation.Events;

public class TransactionFailedDomainEventHandler(ILogger<TransactionFailedDomainEventHandler> logger)
    : INotificationHandler<TransactionFailedDomainEvent>
{
    public Task Handle(TransactionFailedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Transaction {TransactionId} failed for contact {ContactId}",
            notification.TransactionId,
            notification.ContactId);

        return Task.CompletedTask;
    }
}
