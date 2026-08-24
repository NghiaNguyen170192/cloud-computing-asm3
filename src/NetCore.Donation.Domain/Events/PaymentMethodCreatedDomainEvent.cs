using MediatR;
using NetCore.Donation.Domain.Enums;

namespace NetCore.Donation.Domain.Events;

public sealed record PaymentMethodCreatedDomainEvent(
    Guid PaymentMethodId,
    Guid ContactId,
    string DisplayName,
    PaymentType PaymentType) : INotification;
