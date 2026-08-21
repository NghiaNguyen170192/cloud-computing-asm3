using System.Text.Json.Serialization;

namespace NetCore.Donation.Application.Receipt.DTOs;

public sealed record QueryReceiptDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("contact-id")]
    public Guid ContactId { get; set; }

    [JsonPropertyName("contact-full-name")]
    public string ContactFullName { get; set; } = string.Empty;

    [JsonPropertyName("transaction-id")]
    public Guid? TransactionId { get; set; }

    [JsonPropertyName("transaction-identifier")]
    public string? TransactionIdentifier { get; set; }

    [JsonPropertyName("payment-schedule-id")]
    public Guid? PaymentScheduleId { get; set; }

    [JsonPropertyName("payment-schedule-identifier")]
    public string? PaymentScheduleIdentifier { get; set; }

    [JsonPropertyName("document-object-key")]
    public string? DocumentObjectKey { get; set; }

    [JsonPropertyName("document-file-name")]
    public string? DocumentFileName { get; set; }

    [JsonPropertyName("document-content-type")]
    public string? DocumentContentType { get; set; }

    [JsonPropertyName("document-generated-at-utc")]
    public DateTime? DocumentGeneratedAtUtc { get; set; }

    [JsonPropertyName("document-size-bytes")]
    public long? DocumentSizeBytes { get; set; }

    [JsonPropertyName("has-document")]
    public bool HasDocument { get; set; }
}
