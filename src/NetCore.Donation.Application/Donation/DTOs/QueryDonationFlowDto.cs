using System.Text.Json.Serialization;

namespace NetCore.Donation.Application.Donation.DTOs;

public sealed record QueryDonationFlowDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("payment-schedule-id")]
    public Guid? PaymentScheduleId { get; set; }

    [JsonPropertyName("payment-schedule-identifier")]
    public string? PaymentScheduleIdentifier { get; set; }

    [JsonPropertyName("contact-id")]
    public Guid? ContactId { get; set; }

    [JsonPropertyName("contact-email")]
    public string? ContactEmail { get; set; }

    [JsonPropertyName("payment-method-id")]
    public Guid? PaymentMethodId { get; set; }

    [JsonPropertyName("payment-method-display-name")]
    public string? PaymentMethodDisplayName { get; set; }

    [JsonPropertyName("transaction-id")]
    public Guid? TransactionId { get; set; }

    [JsonPropertyName("transaction-identifier")]
    public string? TransactionIdentifier { get; set; }

    [JsonPropertyName("journal-id")]
    public Guid? JournalId { get; set; }

    [JsonPropertyName("journal-identifier")]
    public string? JournalIdentifier { get; set; }

    [JsonPropertyName("receipt-id")]
    public Guid? ReceiptId { get; set; }

    [JsonPropertyName("receipt-identifier")]
    public string? ReceiptIdentifier { get; set; }

    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("money-path")]
    public string MoneyPath { get; set; } = string.Empty;

    [JsonPropertyName("started-at-utc")]
    public DateTime StartedAtUtc { get; set; }

    [JsonPropertyName("last-event-at-utc")]
    public DateTime LastEventAtUtc { get; set; }

    [JsonPropertyName("steps")]
    public List<QueryDonationFlowStepDto> Steps { get; set; } = [];
}
