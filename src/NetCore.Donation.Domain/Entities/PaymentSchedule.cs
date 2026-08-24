#nullable disable

using NetCore.Donation.Domain.Enums;
using NetCore.Donation.Domain.Events;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Domain.Entities;

public class PaymentSchedule : Entity, IAggregateRoot
{
    public string Identifier { get; private set; }

    public Guid ContactId { get; private set; }

    public Contact Contact { get; private set; }

    public Guid PaymentMethodId { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public decimal Amount { get; private set; }

    public DateOnly BookDate { get; private set; }

    public PaymentType PaymentType { get; private set; }

    public RecurringInterval RecurringInterval { get; private set; }

    public static PaymentSchedule Create(
        Guid contactId,
        Guid paymentMethodId,
        decimal amount,
        DateOnly bookDate,
        RecurringInterval recurringInterval,
        PaymentType paymentType = PaymentType.Bank)
    {
        Validate(contactId, paymentMethodId, amount, recurringInterval, paymentType);
        var id = Guid.NewGuid();
        var paymentSchedule = new PaymentSchedule
        {
            Id = id,
            Identifier = RecordIdentifier.PaymentSchedule(bookDate, id),
            ContactId = contactId,
            PaymentMethodId = paymentMethodId,
            Amount = amount,
            BookDate = bookDate,
            PaymentType = paymentType,
            RecurringInterval = recurringInterval,
        };

        paymentSchedule.AddDomainEvent(new PaymentScheduleCreatedDomainEvent(
            paymentSchedule.Id,
            paymentSchedule.Identifier,
            paymentSchedule.ContactId,
            paymentSchedule.PaymentMethodId,
            paymentSchedule.Amount,
            paymentSchedule.PaymentType,
            true,
            paymentSchedule.RecurringInterval));
        return paymentSchedule;
    }

    public void UpdateSchedule(
        Guid paymentMethodId,
        decimal amount,
        DateOnly bookDate,
        RecurringInterval recurringInterval,
        PaymentType paymentType)
    {
        Validate(ContactId, paymentMethodId, amount, recurringInterval, paymentType);
        PaymentMethodId = paymentMethodId;
        Amount = amount;
        BookDate = bookDate;
        RecurringInterval = recurringInterval;
        PaymentType = paymentType;
    }

    private static void Validate(
        Guid contactId,
        Guid paymentMethodId,
        decimal amount,
        RecurringInterval recurringInterval,
        PaymentType paymentType)
    {
        if (contactId == Guid.Empty)
        {
            throw new ArgumentException("Contact ID is required.", nameof(contactId));
        }

        if (paymentMethodId == Guid.Empty)
        {
            throw new ArgumentException("Payment method ID is required.", nameof(paymentMethodId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }

        if (!Enum.IsDefined(recurringInterval) || recurringInterval == RecurringInterval.OneOff)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recurringInterval),
                "Payment schedules are for recurring donations only.");
        }

        if (!Enum.IsDefined(paymentType))
        {
            throw new ArgumentOutOfRangeException(nameof(paymentType));
        }
    }
}
