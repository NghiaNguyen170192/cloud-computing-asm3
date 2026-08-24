using MediatR;

namespace NetCore.Donation.Domain.Events;

public sealed record ReceiptCreatedDomainEvent(
    Guid ReceiptId,
    string Identifier,
    Guid ContactId,
    Guid? TransactionId,
    Guid? PaymentScheduleId) : INotification;
