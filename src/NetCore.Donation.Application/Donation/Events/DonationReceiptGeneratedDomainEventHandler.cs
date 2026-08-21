using MediatR;
using Microsoft.Extensions.Logging;
using NetCore.Donation.Domain.Events;

namespace NetCore.Donation.Application.Donation.Events;

public class DonationReceiptGeneratedDomainEventHandler(ILogger<DonationReceiptGeneratedDomainEventHandler> logger)
    : INotificationHandler<DonationReceiptGeneratedDomainEvent>
{
    public Task Handle(DonationReceiptGeneratedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Donation receipt {ReceiptId} generated for transaction {TransactionId}",
            notification.ReceiptId,
            notification.TransactionId);

        return Task.CompletedTask;
    }
}
