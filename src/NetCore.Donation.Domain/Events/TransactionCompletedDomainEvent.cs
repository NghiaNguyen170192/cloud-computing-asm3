using MediatR;

namespace NetCore.Donation.Domain.Events;

public sealed record TransactionCompletedDomainEvent(
    Guid TransactionId,
    string Identifier,
    Guid ContactId,
    Guid? PaymentScheduleId,
    decimal Amount) : INotification;
