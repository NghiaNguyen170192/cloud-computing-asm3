using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Journal.Delete;

public class DeleteJournalCommandHandler(
    IUnitOfWork unitOfWork,
    IJournalRepository journalRepository)
    : IRequestHandler<DeleteJournalCommand, bool>
{
    public async Task<bool> Handle(DeleteJournalCommand request, CancellationToken cancellationToken)
    {
        var journal = await journalRepository.FindByIdAsync(request.Id, cancellationToken);
        if (journal is null)
        {
            return false;
        }

        journalRepository.Delete(journal);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
