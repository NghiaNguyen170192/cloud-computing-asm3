using MediatR;
using Microsoft.Extensions.Logging;
using NetCore.Donation.Application.Donation.CompleteDonationTransaction;
using NetCore.Donation.Domain.Events;

namespace NetCore.Donation.Application.Donation.Events;

public class TransactionPendingDomainEventHandler(
    IMediator mediator,
    ILogger<TransactionPendingDomainEventHandler> logger)
    : INotificationHandler<TransactionPendingDomainEvent>
{
    public async Task Handle(TransactionPendingDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Transaction {TransactionId} is pending ({DonationKind})",
            notification.TransactionId,
            notification.IsRecurring ? "recurring" : "one-off");

        await mediator.Send(
            new CompleteDonationTransactionCommand(notification.TransactionId),
            cancellationToken);
    }
}
