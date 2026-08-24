using MediatR;
using NetCore.Donation.Domain.Enums;

namespace NetCore.Donation.Domain.Events;

public sealed record PaymentScheduleCreatedDomainEvent(
    Guid PaymentScheduleId,
    string Identifier,
    Guid ContactId,
    Guid PaymentMethodId,
    decimal Amount,
    PaymentType PaymentType,
    bool IsRecurring,
    RecurringInterval RecurringInterval) : INotification;
