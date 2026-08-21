using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Journal.Create;

public class CreateJournalCommandHandler(
    IUnitOfWork unitOfWork,
    IJournalRepository journalRepository,
    ITransactionRepository transactionRepository)
    : IRequestHandler<CreateJournalCommand, Guid>
{
    public async Task<Guid> Handle(CreateJournalCommand request, CancellationToken cancellationToken)
    {
        if (!await transactionRepository.IsExistAsync(request.TransactionId, cancellationToken))
        {
            throw new ArgumentException($"Transaction '{request.TransactionId}' was not found.", nameof(request));
        }

        var journal = request.ToDbEntity();

        await journalRepository.AddAsync(journal, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return journal.Id;
    }
}
