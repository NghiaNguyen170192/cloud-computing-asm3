using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.PaymentSchedule.Create;

public class CreatePaymentScheduleCommandHandler(
    IUnitOfWork unitOfWork,
    IPaymentScheduleRepository paymentScheduleRepository,
    IContactRepository contactRepository,
    IPaymentMethodRepository paymentMethodRepository)
    : IRequestHandler<CreatePaymentScheduleCommand, Guid>
{
    public async Task<Guid> Handle(CreatePaymentScheduleCommand request, CancellationToken cancellationToken)
    {
        if (!await contactRepository.IsExistAsync(request.ContactId, cancellationToken))
        {
            throw new ArgumentException($"Contact '{request.ContactId}' was not found.", nameof(request));
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

        if (paymentMethod.ContactId != request.ContactId)
        {
            throw new InvalidOperationException("The payment method does not belong to the contact.");
        }

        var paymentSchedule = request.ToDbEntity();

        await paymentScheduleRepository.AddAsync(paymentSchedule, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return paymentSchedule.Id;
    }
}