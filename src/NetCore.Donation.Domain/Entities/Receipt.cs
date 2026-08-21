#nullable disable

using NetCore.Donation.Domain.Events;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Domain.Entities;

public class Receipt : Entity, IAggregateRoot
{
    public string Identifier { get; private set; }

    public Guid ContactId { get; private set; }

    public Contact Contact { get; private set; }

    public Guid? TransactionId { get; private set; }

    public Transaction Transaction { get; private set; }

    public Guid? PaymentScheduleId { get; private set; }

    public PaymentSchedule PaymentSchedule { get; private set; }

    public string DocumentObjectKey { get; private set; }

    public string DocumentFileName { get; private set; }

    public string DocumentContentType { get; private set; }

    public DateTime? DocumentGeneratedAtUtc { get; private set; }

    public long? DocumentSizeBytes { get; private set; }

    public static Receipt Create(Guid contactId, Guid? transactionId = null, Guid? paymentScheduleId = null)
    {
        Validate(contactId, transactionId, paymentScheduleId);
        var id = Guid.NewGuid();
        return new Receipt
        {
            Id = id,
            Identifier = RecordIdentifier.Receipt(DateOnly.FromDateTime(DateTime.UtcNow), id),
            ContactId = contactId,
            TransactionId = transactionId,
            PaymentScheduleId = paymentScheduleId,
        };
    }

    public void AssignTransaction(Guid transactionId, Guid? paymentScheduleId = null)
    {
        if (transactionId == Guid.Empty)
        {
            throw new ArgumentException("Transaction ID cannot be empty.", nameof(transactionId));
        }

        if (paymentScheduleId == Guid.Empty)
        {
            throw new ArgumentException("Payment schedule ID cannot be empty.", nameof(paymentScheduleId));
        }

        TransactionId = transactionId;
        PaymentScheduleId = paymentScheduleId;
    }

    public void ClearTransaction()
    {
        TransactionId = null;
        PaymentScheduleId = null;
    }

    public void AssignDocument(
        string objectKey,
        string fileName,
        string contentType,
        long sizeBytes,
        DateTime generatedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        if (sizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Document size cannot be negative.");
        }

        DocumentObjectKey = objectKey.Trim();
        DocumentFileName = fileName.Trim();
        DocumentContentType = contentType.Trim();
        DocumentSizeBytes = sizeBytes;
        DocumentGeneratedAtUtc = generatedAtUtc;
    }

    public void MarkGenerated()
    {
        if (Id == Guid.Empty)
        {
            Id = Guid.NewGuid();
        }

        if (string.IsNullOrWhiteSpace(Identifier))
        {
            Identifier = RecordIdentifier.Receipt(DateOnly.FromDateTime(DateTime.UtcNow), Id);
        }

        AddDomainEvent(new DonationReceiptGeneratedDomainEvent(Id, Identifier, ContactId, TransactionId));
    }

    public void ClearDocument()
    {
        DocumentObjectKey = null;
        DocumentFileName = null;
        DocumentContentType = null;
        DocumentSizeBytes = null;
        DocumentGeneratedAtUtc = null;
    }

    public bool HasDocument => !string.IsNullOrWhiteSpace(DocumentObjectKey);

    private static void Validate(Guid contactId, Guid? transactionId, Guid? paymentScheduleId)
    {
        if (contactId == Guid.Empty)
        {
            throw new ArgumentException("Contact ID is required.", nameof(contactId));
        }

        if (transactionId == Guid.Empty)
        {
            throw new ArgumentException("Transaction ID cannot be empty.", nameof(transactionId));
        }

        if (paymentScheduleId == Guid.Empty)
        {
            throw new ArgumentException("Payment schedule ID cannot be empty.", nameof(paymentScheduleId));
        }
    }
}
