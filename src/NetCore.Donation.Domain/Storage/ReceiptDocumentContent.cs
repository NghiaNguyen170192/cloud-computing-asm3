namespace NetCore.Donation.Domain.Storage;

public sealed record ReceiptDocumentContent(
    Stream Content,
    string ContentType,
    string FileName,
    long SizeBytes);
