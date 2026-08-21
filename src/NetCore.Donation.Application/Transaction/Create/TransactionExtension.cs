namespace NetCore.Donation.Application.Transaction.Create;

public static class TransactionExtension
{
    public static Domain.Entities.Transaction ToDbEntity(this CreateTransactionCommand request)
    {
        return Domain.Entities.Transaction.Create(
            request.Amount,
            request.PaymentScheduleId,
            request.ContactId,
            request.PaymentMethodId,
            request.PaymentType,
            request.BookDate,
            request.ReceivedDate);
    }
}