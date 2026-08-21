namespace NetCore.Donation.Application.PaymentSchedule.Create;

public static class PaymentScheduleExtension
{
    public static Domain.Entities.PaymentSchedule ToDbEntity(this CreatePaymentScheduleCommand request)
    {
        return Domain.Entities.PaymentSchedule.Create(
            request.ContactId,
            request.PaymentMethodId,
            request.Amount,
            request.BookDate,
            request.RecurringInterval,
            request.PaymentType);
    }
}
