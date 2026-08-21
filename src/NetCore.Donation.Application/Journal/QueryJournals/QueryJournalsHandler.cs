using MediatR;
using NetCore.Donation.Application.Journal.DTOs;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Application.Journal.QueryJournals;

public class QueryJournalsHandler(IJournalRepository journalRepository)
    : IRequestHandler<QueryJournals, IQueryable<QueryJournalDto>>
{
    public Task<IQueryable<QueryJournalDto>> Handle(QueryJournals request, CancellationToken cancellationToken)
    {
        return Task.FromResult(journalRepository.GetAll().ToQueryDto());
    }
}
