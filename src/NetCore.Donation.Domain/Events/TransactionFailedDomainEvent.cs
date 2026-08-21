using MediatR;

namespace NetCore.Donation.Domain.Events;

public sealed record TransactionFailedDomainEvent(
    Guid TransactionId,
    string Identifier,
    Guid ContactId,
    decimal Amount) : INotification;
