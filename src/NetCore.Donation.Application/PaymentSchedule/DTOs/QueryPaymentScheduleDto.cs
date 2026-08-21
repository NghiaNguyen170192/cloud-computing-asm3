using NetCore.Donation.Domain.Enums;
using System.Text.Json.Serialization;

namespace NetCore.Donation.Application.PaymentSchedule.DTOs;

public sealed record QueryPaymentScheduleDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonPropertyName("contact-id")]
    public Guid ContactId { get; set; }

    [JsonPropertyName("contact-full-name")]
    public string ContactFullName { get; set; } = string.Empty;

    [JsonPropertyName("payment-method-id")]
    public Guid PaymentMethodId { get; set; }

    [JsonPropertyName("payment-method-display-name")]
    public string PaymentMethodDisplayName { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("book-date")]
    public DateOnly BookDate { get; set; }

    [JsonPropertyName("payment-type")]
    public PaymentType PaymentType { get; set; }

    [JsonPropertyName("recurring-interval")]
    public RecurringInterval RecurringInterval { get; set; }

    [JsonPropertyName("is-recurring")]
    public bool IsRecurring { get; set; }
}
