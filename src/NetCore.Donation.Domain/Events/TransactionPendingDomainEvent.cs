using MediatR;

namespace NetCore.Donation.Domain.Events;

public sealed record TransactionPendingDomainEvent(
    Guid TransactionId,
    string Identifier,
    Guid? PaymentScheduleId,
    Guid ContactId,
    decimal Amount,
    bool IsRecurring) : INotification;
