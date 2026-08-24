#nullable disable

using NetCore.Donation.Domain.Enums;
using NetCore.Donation.Domain.Events;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Domain.Entities;

public class PaymentMethod : Entity, IAggregateRoot
{
    public Guid ContactId { get; private set; }

    public Contact Contact { get; private set; }

    public string DisplayName { get; private set; }

    public PaymentType PaymentType { get; private set; }

    public static PaymentMethod Create(
        Guid contactId,
        string displayName,
        PaymentType paymentType = PaymentType.Bank)
    {
        Validate(contactId, displayName, paymentType);
        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            ContactId = contactId,
            DisplayName = displayName.Trim(),
            PaymentType = paymentType,
        };

        paymentMethod.AddDomainEvent(new PaymentMethodCreatedDomainEvent(
            paymentMethod.Id,
            paymentMethod.ContactId,
            paymentMethod.DisplayName,
            paymentMethod.PaymentType));
        return paymentMethod;
    }

    public void UpdateDetails(string displayName, PaymentType paymentType)
    {
        Validate(ContactId, displayName, paymentType);
        DisplayName = displayName.Trim();
        PaymentType = paymentType;
    }

    private static void Validate(Guid contactId, string displayName, PaymentType paymentType)
    {
        if (contactId == Guid.Empty)
        {
            throw new ArgumentException("Contact ID is required.", nameof(contactId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        if (!Enum.IsDefined(paymentType))
        {
            throw new ArgumentOutOfRangeException(nameof(paymentType));
        }
    }
}
