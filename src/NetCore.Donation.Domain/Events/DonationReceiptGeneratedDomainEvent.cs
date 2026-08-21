using MediatR;

namespace NetCore.Donation.Domain.Events;

public sealed record DonationReceiptGeneratedDomainEvent(
    Guid ReceiptId,
    string Identifier,
    Guid ContactId,
    Guid? TransactionId) : INotification;
