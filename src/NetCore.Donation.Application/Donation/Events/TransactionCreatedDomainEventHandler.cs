using MediatR;
using Microsoft.Extensions.Logging;
using NetCore.Donation.Application.Donation.QueueTransactionPending;
using NetCore.Donation.Domain.Events;

namespace NetCore.Donation.Application.Donation.Events;

public class TransactionCreatedDomainEventHandler(
    IMediator mediator,
    ILogger<TransactionCreatedDomainEventHandler> logger)
    : INotificationHandler<TransactionCreatedDomainEvent>
{
    public async Task Handle(TransactionCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Transaction created {TransactionId} for contact {ContactId}; transitioning to pending",
            notification.TransactionId,
            notification.ContactId);

        await mediator.Send(new QueueTransactionPendingCommand(notification.TransactionId), cancellationToken);
    }
}
