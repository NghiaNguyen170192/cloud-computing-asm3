using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.PaymentMethod.Delete;

public class DeletePaymentMethodCommandHandler(
    IUnitOfWork unitOfWork,
    IPaymentMethodRepository paymentMethodRepository)
    : IRequestHandler<DeletePaymentMethodCommand, bool>
{
    public async Task<bool> Handle(DeletePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var paymentMethod = await paymentMethodRepository.FindByIdAsync(request.Id, cancellationToken);
        if (paymentMethod is null)
        {
            return false;
        }

        paymentMethodRepository.Delete(paymentMethod);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}