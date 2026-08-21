namespace NetCore.Donation.Application.Receipt.Create;

public static class ReceiptExtension
{
    public static Domain.Entities.Receipt ToDbEntity(
        this CreateReceiptCommand request,
        Guid? paymentScheduleId = null)
    {
        return Domain.Entities.Receipt.Create(request.ContactId, request.TransactionId, paymentScheduleId);
    }
}
