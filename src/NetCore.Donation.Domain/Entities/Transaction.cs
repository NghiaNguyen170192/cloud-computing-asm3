#nullable disable

using NetCore.Donation.Domain.Enums;
using NetCore.Donation.Domain.Events;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Domain.Entities;

public class Transaction : Entity, IAggregateRoot
{
    public string Identifier { get; private set; }

    public decimal Amount { get; private set; }

    public Guid? PaymentScheduleId { get; private set; }

    public PaymentSchedule PaymentSchedule { get; private set; }

    public Guid ContactId { get; private set; }

    public Contact Contact { get; private set; }

    public Guid PaymentMethodId { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public PaymentType PaymentType { get; private set; }

    public TransactionStatus Status { get; private set; }

    public DateOnly BookDate { get; private set; }

    public DateOnly ReceivedDate { get; private set; }

    public ICollection<Journal> Journals { get; private set; } = new List<Journal>();

    public ICollection<Receipt> Receipts { get; private set; } = new List<Receipt>();

    public static Transaction Create(
        decimal amount,
        Guid? paymentScheduleId,
        Guid contactId,
        Guid paymentMethodId,
        PaymentType paymentType,
        DateOnly bookDate,
        DateOnly receivedDate)
    {
        Validate(amount, paymentScheduleId, contactId, paymentMethodId, paymentType, bookDate, receivedDate);
        var id = Guid.NewGuid();
        var transaction = new Transaction
        {
            Id = id,
            Identifier = RecordIdentifier.Transaction(bookDate, id),
            Amount = amount,
            PaymentScheduleId = paymentScheduleId,
            ContactId = contactId,
            PaymentMethodId = paymentMethodId,
            PaymentType = paymentType,
            Status = TransactionStatus.Pending,
            BookDate = bookDate,
            ReceivedDate = receivedDate,
        };

        transaction.RaiseCreated();
        return transaction;
    }

    public static Transaction CreatePending(
        decimal amount,
        Guid? paymentScheduleId,
        Guid contactId,
        Guid paymentMethodId,
        PaymentType paymentType,
        DateOnly bookDate,
        bool isRecurring)
    {
        Validate(amount, paymentScheduleId, contactId, paymentMethodId, paymentType, bookDate, bookDate);
        var id = Guid.NewGuid();
        var transaction = new Transaction
        {
            Id = id,
            Identifier = RecordIdentifier.Transaction(bookDate, id),
            Amount = amount,
            PaymentScheduleId = paymentScheduleId,
            ContactId = contactId,
            PaymentMethodId = paymentMethodId,
            PaymentType = paymentType,
            Status = TransactionStatus.Pending,
            BookDate = bookDate,
            ReceivedDate = bookDate,
        };

        transaction.RaiseCreated();
        return transaction;
    }

    public void TransitionToPending()
    {
        if (Status != TransactionStatus.Pending)
        {
            return;
        }

        AddDomainEvent(new TransactionPendingDomainEvent(
            Id,
            Identifier,
            PaymentScheduleId,
            ContactId,
            Amount,
            PaymentScheduleId is not null));
    }

    public void MarkSucceeded()
    {
        EnsurePending();
        Status = TransactionStatus.Succeeded;
        ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        RaiseCompleted();
    }

    public void MarkFailed()
    {
        EnsurePending();
        Status = TransactionStatus.Failed;
        AddDomainEvent(new TransactionFailedDomainEvent(Id, Identifier, ContactId, Amount));
    }

    public void UpdateReceiptDetails(
        decimal amount,
        Guid paymentMethodId,
        PaymentType paymentType,
        DateOnly bookDate,
        DateOnly receivedDate,
        Guid? paymentScheduleId,
        TransactionStatus status)
    {
        Validate(amount, paymentScheduleId, ContactId, paymentMethodId, paymentType, bookDate, receivedDate);
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        Amount = amount;
        PaymentMethodId = paymentMethodId;
        PaymentType = paymentType;
        BookDate = bookDate;
        ReceivedDate = receivedDate;
        PaymentScheduleId = paymentScheduleId;
        Status = status;
    }

    private void EnsurePending()
    {
        if (Status != TransactionStatus.Pending)
        {
            throw new InvalidOperationException($"Transaction '{Id}' cannot leave status '{Status}'.");
        }

        if (Id == Guid.Empty)
        {
            Id = Guid.NewGuid();
        }

        if (string.IsNullOrWhiteSpace(Identifier))
        {
            Identifier = RecordIdentifier.Transaction(BookDate, Id);
        }
    }

    private void RaiseCreated()
    {
        AddDomainEvent(new TransactionCreatedDomainEvent(
            Id,
            Identifier,
            ContactId,
            PaymentScheduleId,
            PaymentMethodId,
            Amount));
    }

    private void RaiseCompleted()
    {
        AddDomainEvent(new TransactionCompletedDomainEvent(
            Id,
            Identifier,
            ContactId,
            PaymentScheduleId,
            Amount));
    }

    private static void Validate(
        decimal amount,
        Guid? paymentScheduleId,
        Guid contactId,
        Guid paymentMethodId,
        PaymentType paymentType,
        DateOnly bookDate,
        DateOnly receivedDate)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }

        if (paymentScheduleId == Guid.Empty)
        {
            throw new ArgumentException("Payment schedule ID cannot be empty.", nameof(paymentScheduleId));
        }

        if (contactId == Guid.Empty)
        {
            throw new ArgumentException("Contact ID is required.", nameof(contactId));
        }

        if (paymentMethodId == Guid.Empty)
        {
            throw new ArgumentException("Payment method ID is required.", nameof(paymentMethodId));
        }

        if (!Enum.IsDefined(paymentType))
        {
            throw new ArgumentOutOfRangeException(nameof(paymentType));
        }

        if (receivedDate < bookDate)
        {
            throw new ArgumentOutOfRangeException(nameof(receivedDate), "Received date cannot precede book date.");
        }
    }
}