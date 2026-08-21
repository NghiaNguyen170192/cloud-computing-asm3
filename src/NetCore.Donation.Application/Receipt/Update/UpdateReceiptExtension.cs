namespace NetCore.Donation.Application.Receipt.Update;

public static class UpdateReceiptExtension
{
    public static void UpdateEntity(
        this UpdateReceiptCommand request,
        Domain.Entities.Receipt receipt,
        Guid? paymentScheduleId)
    {
        if (request.TransactionId is { } transactionId)
        {
            receipt.AssignTransaction(transactionId, paymentScheduleId);
        }
        else
        {
            receipt.ClearTransaction();
        }
    }
}
