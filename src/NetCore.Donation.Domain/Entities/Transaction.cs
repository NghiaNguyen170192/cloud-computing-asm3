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
        return new Transaction
        {
            Id = id,
            Identifier = RecordIdentifier.Transaction(bookDate, id),
            Amount = amount,
            PaymentScheduleId = paymentScheduleId,
            ContactId = contactId,
            PaymentMethodId = paymentMethodId,
            PaymentType = paymentType,
            Status = TransactionStatus.Succeeded,
            BookDate = bookDate,
            ReceivedDate = receivedDate,
        };
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

        transaction.AddDomainEvent(new TransactionPendingDomainEvent(
            transaction.Id,
            transaction.Identifier,
            transaction.PaymentScheduleId,
            transaction.ContactId,
            transaction.Amount,
            isRecurring));
        return transaction;
    }

    public void MarkSucceeded()
    {
        EnsurePending();
        Status = TransactionStatus.Succeeded;
        ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        AddDomainEvent(new TransactionSucceededDomainEvent(Id, Identifier, ContactId, PaymentScheduleId, Amount));
    }

    public void MarkFailed()
    {
        EnsurePending();
        Status = TransactionStatus.Failed;
        AddDomainEvent(new TransactionFailedDomainEvent(Id, Identifier, ContactId, Amount));
    }

    public void UpdateReceiptDetails(decimal amount, PaymentType paymentType, DateOnly receivedDate)
    {
        Validate(amount, PaymentScheduleId, ContactId, PaymentMethodId, paymentType, BookDate, receivedDate);
        Amount = amount;
        PaymentType = paymentType;
        ReceivedDate = receivedDate;
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
