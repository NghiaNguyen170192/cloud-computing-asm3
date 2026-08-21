using MediatR;
using Microsoft.Extensions.Logging;
using NetCore.Donation.Domain.Events;

namespace NetCore.Donation.Application.Donation.Events;

public class DonationPaymentMethodCreatedDomainEventHandler(
    ILogger<DonationPaymentMethodCreatedDomainEventHandler> logger)
    : INotificationHandler<DonationPaymentMethodCreatedDomainEvent>
{
    public Task Handle(DonationPaymentMethodCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Donation payment method created {PaymentMethodId} for contact {ContactId}",
            notification.PaymentMethodId,
            notification.ContactId);

        return Task.CompletedTask;
    }
}
