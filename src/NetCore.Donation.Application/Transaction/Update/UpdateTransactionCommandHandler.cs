using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Transaction.Update;

public class UpdateTransactionCommandHandler(
    IUnitOfWork unitOfWork,
    ITransactionRepository transactionRepository,
    IPaymentMethodRepository paymentMethodRepository,
    IPaymentScheduleRepository paymentScheduleRepository)
    : IRequestHandler<UpdateTransactionCommand, bool>
{
    public async Task<bool> Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.FindByIdAsync(request.Id, cancellationToken);
        if (transaction is null)
        {
            return false;
        }

        var paymentMethod = await paymentMethodRepository.FindByIdAsync(request.PaymentMethodId, cancellationToken);
        if (paymentMethod is null)
        {
            throw new ArgumentException($"Payment method '{request.PaymentMethodId}' was not found.", nameof(request));
        }

        if (paymentMethod.ContactId != transaction.ContactId)
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

            if (paymentSchedule.ContactId != transaction.ContactId)
            {
                throw new InvalidOperationException("The payment schedule must belong to the contact.");
            }
        }

        transaction.UpdateReceiptDetails(
            request.Amount,
            request.PaymentMethodId,
            paymentMethod.PaymentType,
            request.BookDate,
            request.ReceivedDate,
            request.PaymentScheduleId,
            request.Status);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}