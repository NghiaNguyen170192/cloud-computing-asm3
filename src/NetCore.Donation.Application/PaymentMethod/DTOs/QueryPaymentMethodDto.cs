using System.Text.Json.Serialization;
using NetCore.Donation.Domain.Enums;

namespace NetCore.Donation.Application.PaymentMethod.DTOs;

public sealed record QueryPaymentMethodDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("contact-id")]
    public Guid ContactId { get; set; }

    [JsonPropertyName("contact-full-name")]
    public string ContactFullName { get; set; } = string.Empty;

    [JsonPropertyName("display-name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("payment-type")]
    public PaymentType PaymentType { get; set; }
}
