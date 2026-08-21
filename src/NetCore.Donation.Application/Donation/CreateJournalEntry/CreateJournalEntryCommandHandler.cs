using MediatR;
using NetCore.Donation.Domain.Enums;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Donation.CreateJournalEntry;

public class CreateJournalEntryCommandHandler(
    IUnitOfWork unitOfWork,
    IJournalRepository journalRepository,
    ITransactionRepository transactionRepository)
    : IRequestHandler<CreateJournalEntryCommand, Guid>
{
    public async Task<Guid> Handle(CreateJournalEntryCommand request, CancellationToken cancellationToken)
    {
        var existing = await journalRepository.FindByTransactionIdAsync(request.TransactionId, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        var transaction = await transactionRepository.FindByIdAsync(request.TransactionId, cancellationToken);
        if (transaction is null)
        {
            throw new ArgumentException($"Transaction '{request.TransactionId}' was not found.", nameof(request));
        }

        if (transaction.Status != TransactionStatus.Succeeded)
        {
            throw new InvalidOperationException(
                $"Journal entries can only be created for succeeded transactions. Status is '{transaction.Status}'.");
        }

        var journal = Domain.Entities.Journal.Create(request.TransactionId);
        await journalRepository.AddAsync(journal, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return journal.Id;
    }
}
