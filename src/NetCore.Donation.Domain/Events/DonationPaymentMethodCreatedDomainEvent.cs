using MediatR;
using NetCore.Donation.Domain.Enums;

namespace NetCore.Donation.Domain.Events;

public sealed record DonationPaymentMethodCreatedDomainEvent(
    Guid PaymentMethodId,
    Guid ContactId,
    string DisplayName,
    PaymentType PaymentType) : INotification;
