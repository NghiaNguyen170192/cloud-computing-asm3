using MediatR;
using Microsoft.Extensions.Logging;
using NetCore.Donation.Domain.Events;

namespace NetCore.Donation.Application.Donation.Events;

public class JournalEntryCreatedDomainEventHandler(ILogger<JournalEntryCreatedDomainEventHandler> logger)
    : INotificationHandler<JournalEntryCreatedDomainEvent>
{
    public Task Handle(JournalEntryCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Journal entry {JournalId} created for transaction {TransactionId}",
            notification.JournalId,
            notification.TransactionId);

        return Task.CompletedTask;
    }
}
