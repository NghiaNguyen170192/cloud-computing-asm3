namespace NetCore.Donation.Domain.Messaging;

public sealed record IntegrationEvent(
    string MessageType,
    string Payload,
    string CorrelationId,
    string IdempotencyKey);
