using MediatR;
using NetCore.Donation.Domain.Enums;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Donation.QueueTransactionPending;

public class QueueTransactionPendingCommandHandler(
    IUnitOfWork unitOfWork,
    ITransactionRepository transactionRepository)
    : IRequestHandler<QueueTransactionPendingCommand, Guid>
{
    public async Task<Guid> Handle(QueueTransactionPendingCommand request, CancellationToken cancellationToken)
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

        transaction.TransitionToPending();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return transaction.Id;
    }
}
