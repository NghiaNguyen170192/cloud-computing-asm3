using MediatR;
using NetCore.Donation.Application.Outbox.DTOs;

namespace NetCore.Donation.Application.Outbox.QueryOutboxMessages;

public sealed record QueryOutboxMessages(string? CorrelationId = null, string? IdempotencyKey = null)
    : IRequest<IQueryable<QueryOutboxMessageDto>>;
