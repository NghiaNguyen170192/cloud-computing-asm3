using System.Text.Json.Serialization;

namespace NetCore.Donation.Application.Donation.DTOs;

public sealed record QueryDonationFlowStepDto
{
    [JsonPropertyName("event-name")]
    public string EventName { get; set; } = string.Empty;

    [JsonPropertyName("occurred-at-utc")]
    public DateTime OccurredAtUtc { get; set; }

    [JsonPropertyName("processed-at-utc")]
    public DateTime? ProcessedAtUtc { get; set; }

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("correlation-id")]
    public string CorrelationId { get; set; } = string.Empty;
}
