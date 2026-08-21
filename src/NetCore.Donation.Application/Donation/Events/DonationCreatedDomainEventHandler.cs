using MediatR;
using Microsoft.Extensions.Logging;
using NetCore.Donation.Application.Donation.ProcessDonationTransaction;
using NetCore.Donation.Domain.Events;

namespace NetCore.Donation.Application.Donation.Events;

public class DonationCreatedDomainEventHandler(
    IMediator mediator,
    ILogger<DonationCreatedDomainEventHandler> logger)
    : INotificationHandler<DonationCreatedDomainEvent>
{
    public async Task Handle(DonationCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Donation created on schedule {PaymentScheduleId} for contact {ContactId} ({DonationKind})",
            notification.PaymentScheduleId,
            notification.ContactId,
            "recurring");

        await mediator.Send(
            new ProcessDonationTransactionCommand(
                notification.PaymentScheduleId,
                notification.ContactId,
                notification.PaymentMethodId,
                notification.Amount,
                notification.PaymentType,
                notification.IsRecurring,
                notification.RecurringInterval),
            cancellationToken);
    }
}
