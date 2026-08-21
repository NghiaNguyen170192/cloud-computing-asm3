#nullable disable

using NetCore.Donation.Domain.Events;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Domain.Entities;

public class Journal : Entity, IAggregateRoot
{
    public string Identifier { get; private set; }

    public Guid TransactionId { get; private set; }

    public Transaction Transaction { get; private set; }

    public static Journal Create(Guid transactionId)
    {
        if (transactionId == Guid.Empty)
        {
            throw new ArgumentException("Transaction ID is required.", nameof(transactionId));
        }

        var id = Guid.NewGuid();
        var journal = new Journal
        {
            Id = id,
            Identifier = RecordIdentifier.Journal(DateOnly.FromDateTime(DateTime.UtcNow), id),
            TransactionId = transactionId,
        };

        journal.AddDomainEvent(new JournalEntryCreatedDomainEvent(
            journal.Id,
            journal.Identifier,
            journal.TransactionId));
        return journal;
    }
}
