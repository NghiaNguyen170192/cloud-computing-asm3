namespace NetCore.Donation.Application.Transaction.Update;

public static class UpdateTransactionExtension
{
    public static void UpdateEntity(
        this UpdateTransactionCommand request,
        Domain.Entities.Transaction transaction)
    {
        transaction.UpdateReceiptDetails(
            request.Amount,
            request.PaymentMethodId,
            request.PaymentType,
            request.BookDate,
            request.ReceivedDate,
            request.PaymentScheduleId,
            request.Status);
    }
}