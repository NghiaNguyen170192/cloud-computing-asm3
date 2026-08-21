using NetCore.Donation.Application.Outbox.DTOs;
using NetCore.Donation.Domain.Entities;

namespace NetCore.Donation.Application.Outbox.DTOs;

public static class QueryOutboxMessageDtoExtension
{
    public static IQueryable<QueryOutboxMessageDto> ToQueryDto(this IQueryable<OutboxMessage> messages)
    {
        return messages.Select(message => new QueryOutboxMessageDto
        {
            Id = message.Id,
            MessageType = message.MessageType,
            Payload = message.Payload,
            CorrelationId = message.CorrelationId,
            IdempotencyKey = message.IdempotencyKey,
            OccurredAtUtc = message.OccurredAtUtc,
            ProcessedAtUtc = message.ProcessedAtUtc,
            AttemptCount = message.AttemptCount,
            LastError = message.LastError,
        });
    }
}
