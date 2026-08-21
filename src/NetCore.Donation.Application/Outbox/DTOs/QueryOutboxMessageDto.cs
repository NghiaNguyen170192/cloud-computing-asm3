using System.Text.Json.Serialization;

namespace NetCore.Donation.Application.Outbox.DTOs;

public sealed record QueryOutboxMessageDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("message-type")]
    public string MessageType { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public string Payload { get; set; } = string.Empty;

    [JsonPropertyName("correlation-id")]
    public string CorrelationId { get; set; } = string.Empty;

    [JsonPropertyName("idempotency-key")]
    public string IdempotencyKey { get; set; } = string.Empty;

    [JsonPropertyName("occurred-at-utc")]
    public DateTime OccurredAtUtc { get; set; }

    [JsonPropertyName("processed-at-utc")]
    public DateTime? ProcessedAtUtc { get; set; }

    [JsonPropertyName("attempt-count")]
    public int AttemptCount { get; set; }

    [JsonPropertyName("last-error")]
    public string? LastError { get; set; }
}
