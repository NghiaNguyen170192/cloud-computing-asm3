namespace NetCore.Donation.Application.Transaction.DTOs;

public static class QueryTransactionDtoExtension
{
    public static IQueryable<QueryTransactionDto> ToQueryDto(this IQueryable<Domain.Entities.Transaction> transactions)
    {
        return transactions.Select(transaction => new QueryTransactionDto
        {
            Id = transaction.Id,
            Identifier = transaction.Identifier,
            Amount = transaction.Amount,
            PaymentScheduleId = transaction.PaymentScheduleId,
            PaymentScheduleIdentifier = transaction.PaymentSchedule != null ? transaction.PaymentSchedule.Identifier : null,
            ContactId = transaction.ContactId,
            ContactFullName = transaction.Contact.FirstName + " " + transaction.Contact.LastName,
            PaymentMethodId = transaction.PaymentMethodId,
            PaymentMethodDisplayName = transaction.PaymentMethod.DisplayName,
            PaymentType = transaction.PaymentType,
            Status = transaction.Status,
            BookDate = transaction.BookDate,
            ReceivedDate = transaction.ReceivedDate,
        });
    }
}
