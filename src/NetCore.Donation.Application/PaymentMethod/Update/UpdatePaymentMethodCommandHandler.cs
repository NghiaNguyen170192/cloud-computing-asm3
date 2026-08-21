using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.PaymentMethod.Update;

public class UpdatePaymentMethodCommandHandler(
    IUnitOfWork unitOfWork,
    IPaymentMethodRepository paymentMethodRepository)
    : IRequestHandler<UpdatePaymentMethodCommand, bool>
{
    public async Task<bool> Handle(UpdatePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var paymentMethod = await paymentMethodRepository.FindByIdAsync(request.Id, cancellationToken);
        if (paymentMethod is null)
        {
            return false;
        }

        request.UpdateEntity(paymentMethod);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}