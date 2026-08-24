using MediatR;
using Microsoft.Extensions.Logging;
using NetCore.Donation.Application.Donation.ProcessDonationTransaction;
using NetCore.Donation.Domain.Events;

namespace NetCore.Donation.Application.Donation.Events;

public class PaymentScheduleCreatedDomainEventHandler(
    IMediator mediator,
    ILogger<PaymentScheduleCreatedDomainEventHandler> logger)
    : INotificationHandler<PaymentScheduleCreatedDomainEvent>
{
    public async Task Handle(PaymentScheduleCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Payment schedule created {PaymentScheduleId} for contact {ContactId}",
            notification.PaymentScheduleId,
            notification.ContactId);

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
