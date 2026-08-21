using MediatR;
using NetCore.Donation.Application.Outbox.DTOs;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Application.Outbox.QueryOutboxMessages;

public class QueryOutboxMessagesHandler(IOutboxMessageRepository outboxMessageRepository)
    : IRequestHandler<QueryOutboxMessages, IQueryable<QueryOutboxMessageDto>>
{
    public Task<IQueryable<QueryOutboxMessageDto>> Handle(
        QueryOutboxMessages request,
        CancellationToken cancellationToken)
    {
        var query = outboxMessageRepository.GetAll().ToQueryDto();
        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            query = query.Where(message => message.CorrelationId == request.CorrelationId);
        }

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            query = query.Where(message => message.IdempotencyKey == request.IdempotencyKey);
        }

        return Task.FromResult(query);
    }
}
