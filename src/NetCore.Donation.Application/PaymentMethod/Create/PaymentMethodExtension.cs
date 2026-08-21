namespace NetCore.Donation.Application.PaymentMethod.Create;

public static class PaymentMethodExtension
{
    public static Domain.Entities.PaymentMethod ToDbEntity(this CreatePaymentMethodCommand request)
    {
        return Domain.Entities.PaymentMethod.Create(
            request.ContactId,
            request.DisplayName,
            request.PaymentType);
    }
}
