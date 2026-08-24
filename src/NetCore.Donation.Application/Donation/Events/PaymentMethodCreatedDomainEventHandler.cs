using MediatR;
using Microsoft.Extensions.Logging;
using NetCore.Donation.Domain.Events;

namespace NetCore.Donation.Application.Donation.Events;

public class PaymentMethodCreatedDomainEventHandler(
    ILogger<PaymentMethodCreatedDomainEventHandler> logger)
    : INotificationHandler<PaymentMethodCreatedDomainEvent>
{
    public Task Handle(PaymentMethodCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Payment method created {PaymentMethodId} for contact {ContactId}",
            notification.PaymentMethodId,
            notification.ContactId);

        return Task.CompletedTask;
    }
}
