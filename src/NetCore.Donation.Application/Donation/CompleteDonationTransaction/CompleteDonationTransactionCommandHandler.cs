using MediatR;
using NetCore.Donation.Domain.Enums;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Donation.CompleteDonationTransaction;

public class CompleteDonationTransactionCommandHandler(
    IUnitOfWork unitOfWork,
    ITransactionRepository transactionRepository,
    IDonationTransactionOutcome outcome)
    : IRequestHandler<CompleteDonationTransactionCommand, Guid>
{
    public async Task<Guid> Handle(CompleteDonationTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.FindByIdAsync(request.TransactionId, cancellationToken);
        if (transaction is null)
        {
            throw new ArgumentException($"Transaction '{request.TransactionId}' was not found.", nameof(request));
        }

        if (transaction.Status is TransactionStatus.Succeeded or TransactionStatus.Failed)
        {
            return transaction.Id;
        }

        if (outcome.IsSuccess())
        {
            transaction.MarkSucceeded();
        }
        else
        {
            transaction.MarkFailed();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return transaction.Id;
    }
}
