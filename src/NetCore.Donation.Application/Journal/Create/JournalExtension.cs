namespace NetCore.Donation.Application.Journal.Create;

public static class JournalExtension
{
    public static Domain.Entities.Journal ToDbEntity(this CreateJournalCommand request)
    {
        return Domain.Entities.Journal.Create(request.TransactionId);
    }
}
