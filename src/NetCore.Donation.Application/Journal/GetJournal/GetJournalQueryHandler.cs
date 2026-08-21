using MediatR;
using NetCore.Donation.Application.Journal.DTOs;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Application.Journal.GetJournal;

public class GetJournalQueryHandler(IJournalRepository journalRepository)
    : IRequestHandler<GetJournalQuery, QueryJournalDto?>
{
    public Task<QueryJournalDto?> Handle(GetJournalQuery request, CancellationToken cancellationToken)
    {
        var journal = journalRepository.GetAll()
            .ToQueryDto()
            .FirstOrDefault(journal => journal.Id == request.Id);

        return Task.FromResult(journal);
    }
}
