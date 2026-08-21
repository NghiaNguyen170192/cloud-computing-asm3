using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Transaction.Update;

public class UpdateTransactionCommandHandler(
    IUnitOfWork unitOfWork,
    ITransactionRepository transactionRepository)
    : IRequestHandler<UpdateTransactionCommand, bool>
{
    public async Task<bool> Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.FindByIdAsync(request.Id, cancellationToken);
        if (transaction is null)
        {
            return false;
        }

        request.UpdateEntity(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}