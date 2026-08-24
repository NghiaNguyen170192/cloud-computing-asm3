using MediatR;

namespace NetCore.Donation.Domain.Events;

public sealed record TransactionCreatedDomainEvent(
    Guid TransactionId,
    string Identifier,
    Guid ContactId,
    Guid? PaymentScheduleId,
    Guid PaymentMethodId,
    decimal Amount) : INotification;
