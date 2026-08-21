namespace NetCore.Donation.Application.Receipt.DTOs;

public static class QueryReceiptDtoExtension
{
    public static IQueryable<QueryReceiptDto> ToQueryDto(this IQueryable<Domain.Entities.Receipt> receipts)
    {
        return receipts.Select(receipt => new QueryReceiptDto
        {
            Id = receipt.Id,
            Identifier = receipt.Identifier,
            ContactId = receipt.ContactId,
            ContactFullName = receipt.Contact.FirstName + " " + receipt.Contact.LastName,
            TransactionId = receipt.TransactionId,
            TransactionIdentifier = receipt.Transaction != null ? receipt.Transaction.Identifier : null,
            PaymentScheduleId = receipt.PaymentScheduleId,
            PaymentScheduleIdentifier = receipt.PaymentSchedule != null ? receipt.PaymentSchedule.Identifier : null,
            DocumentObjectKey = receipt.DocumentObjectKey,
            DocumentFileName = receipt.DocumentFileName,
            DocumentContentType = receipt.DocumentContentType,
            DocumentGeneratedAtUtc = receipt.DocumentGeneratedAtUtc,
            DocumentSizeBytes = receipt.DocumentSizeBytes,
            HasDocument = receipt.DocumentObjectKey != null && receipt.DocumentObjectKey != string.Empty,
        });
    }
}
