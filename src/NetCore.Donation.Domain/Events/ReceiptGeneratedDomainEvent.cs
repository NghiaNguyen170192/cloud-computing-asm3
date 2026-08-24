using MediatR;

namespace NetCore.Donation.Domain.Events;

public sealed record ReceiptGeneratedDomainEvent(
    Guid ReceiptId,
    string Identifier,
    Guid ContactId,
    Guid? TransactionId) : INotification;
