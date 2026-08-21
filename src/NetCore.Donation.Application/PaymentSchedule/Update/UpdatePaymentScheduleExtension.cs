namespace NetCore.Donation.Application.PaymentSchedule.Update;

public static class UpdatePaymentScheduleExtension
{
    public static void UpdateEntity(
        this UpdatePaymentScheduleCommand request,
        Domain.Entities.PaymentSchedule paymentSchedule)
    {
        paymentSchedule.UpdateSchedule(
            request.PaymentMethodId,
            request.Amount,
            request.BookDate,
            request.RecurringInterval,
            request.PaymentType);
    }
}
