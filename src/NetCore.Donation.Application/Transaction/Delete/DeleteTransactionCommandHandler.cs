using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Transaction.Delete;

public class DeleteTransactionCommandHandler(
    IUnitOfWork unitOfWork,
    ITransactionRepository transactionRepository)
    : IRequestHandler<DeleteTransactionCommand, bool>
{
    public async Task<bool> Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.FindByIdAsync(request.Id, cancellationToken);
        if (transaction is null)
        {
            return false;
        }

        transactionRepository.Delete(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}