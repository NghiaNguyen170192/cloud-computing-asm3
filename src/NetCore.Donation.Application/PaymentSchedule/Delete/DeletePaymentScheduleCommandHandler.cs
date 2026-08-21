using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.PaymentSchedule.Delete;

public class DeletePaymentScheduleCommandHandler(
    IUnitOfWork unitOfWork,
    IPaymentScheduleRepository paymentScheduleRepository)
    : IRequestHandler<DeletePaymentScheduleCommand, bool>
{
    public async Task<bool> Handle(DeletePaymentScheduleCommand request, CancellationToken cancellationToken)
    {
        var paymentSchedule = await paymentScheduleRepository.FindByIdAsync(request.Id, cancellationToken);
        if (paymentSchedule is null)
        {
            return false;
        }

        paymentScheduleRepository.Delete(paymentSchedule);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}