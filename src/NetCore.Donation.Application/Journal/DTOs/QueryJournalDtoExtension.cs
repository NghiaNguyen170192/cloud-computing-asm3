namespace NetCore.Donation.Application.Journal.DTOs;

public static class QueryJournalDtoExtension
{
    public static IQueryable<QueryJournalDto> ToQueryDto(this IQueryable<Domain.Entities.Journal> journals)
    {
        return journals.Select(journal => new QueryJournalDto
        {
            Id = journal.Id,
            Identifier = journal.Identifier,
            TransactionId = journal.TransactionId,
            TransactionIdentifier = journal.Transaction.Identifier,
            CreatedDate = journal.CreatedDate,
            ModifiedDate = journal.ModifiedDate,
        });
    }
}
