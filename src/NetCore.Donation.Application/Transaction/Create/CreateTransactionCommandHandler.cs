using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Transaction.Create;

public class CreateTransactionCommandHandler(
    IUnitOfWork unitOfWork,
    ITransactionRepository transactionRepository,
    IContactRepository contactRepository,
    IPaymentMethodRepository paymentMethodRepository,
    IPaymentScheduleRepository paymentScheduleRepository)
    : IRequestHandler<CreateTransactionCommand, Guid>
{
    public async Task<Guid> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        if (!await contactRepository.IsExistAsync(request.ContactId, cancellationToken))
        {
            throw new ArgumentException($"Contact '{request.ContactId}' was not found.", nameof(request));
        }

        var paymentMethod = await paymentMethodRepository.FindByIdAsync(request.PaymentMethodId, cancellationToken);
        if (paymentMethod is null)
        {
            throw new ArgumentException($"Payment method '{request.PaymentMethodId}' was not found.", nameof(request));
        }

        if (paymentMethod.ContactId != request.ContactId)
        {
            throw new InvalidOperationException("The payment method must belong to the contact.");
        }

        if (request.PaymentScheduleId is { } paymentScheduleId)
        {
            var paymentSchedule = await paymentScheduleRepository.FindByIdAsync(paymentScheduleId, cancellationToken);
            if (paymentSchedule is null)
            {
                throw new ArgumentException($"Payment schedule '{paymentScheduleId}' was not found.", nameof(request));
            }

            if (paymentSchedule.ContactId != request.ContactId ||
                paymentSchedule.PaymentMethodId != request.PaymentMethodId)
            {
                throw new InvalidOperationException("The payment method and schedule must belong to the contact and to each other.");
            }
        }

        var transaction = request.ToDbEntity();

        await transactionRepository.AddAsync(transaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction.Id;
    }
}
