namespace NetCore.Donation.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; private set; }

    public string MessageType { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public string CorrelationId { get; private set; } = string.Empty;

    public string IdempotencyKey { get; private set; } = string.Empty;

    public DateTime OccurredAtUtc { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    public int AttemptCount { get; private set; }

    public string? LastError { get; private set; }

    public static OutboxMessage Create(
        string messageType,
        string payload,
        string correlationId,
        string idempotencyKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = messageType,
            Payload = payload,
            CorrelationId = correlationId,
            IdempotencyKey = idempotencyKey,
            OccurredAtUtc = DateTime.UtcNow,
        };
    }

    public void MarkProcessed()
    {
        ProcessedAtUtc = DateTime.UtcNow;
        LastError = null;
    }

    public void RecordFailure(string error)
    {
        AttemptCount++;
        LastError = error.Length > 2000 ? error[..2000] : error;
    }
}
