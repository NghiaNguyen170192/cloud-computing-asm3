using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.PaymentSchedule.Update;

public class UpdatePaymentScheduleCommandHandler(
    IUnitOfWork unitOfWork,
    IPaymentScheduleRepository paymentScheduleRepository,
    IPaymentMethodRepository paymentMethodRepository)
    : IRequestHandler<UpdatePaymentScheduleCommand, bool>
{
    public async Task<bool> Handle(UpdatePaymentScheduleCommand request, CancellationToken cancellationToken)
    {
        var paymentSchedule = await paymentScheduleRepository.FindByIdAsync(request.Id, cancellationToken);
        if (paymentSchedule is null)
        {
            return false;
        }

        var paymentMethod = await paymentMethodRepository.FindByIdAsync(
            request.PaymentMethodId,
            cancellationToken);
        if (paymentMethod is null)
        {
            throw new ArgumentException(
                $"Payment method '{request.PaymentMethodId}' was not found.",
                nameof(request));
        }

        if (paymentMethod.ContactId != paymentSchedule.ContactId)
        {
            throw new InvalidOperationException("The payment method does not belong to the contact.");
        }

        paymentSchedule.UpdateSchedule(
            request.PaymentMethodId,
            request.Amount,
            request.BookDate,
            request.RecurringInterval,
            paymentMethod.PaymentType);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}