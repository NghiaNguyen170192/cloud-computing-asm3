using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Donation.ProcessDonationTransaction;

public class ProcessDonationTransactionCommandHandler(
    IUnitOfWork unitOfWork,
    IPaymentScheduleRepository paymentScheduleRepository,
    ITransactionRepository transactionRepository)
    : IRequestHandler<ProcessDonationTransactionCommand, Guid>
{
    public async Task<Guid> Handle(ProcessDonationTransactionCommand request, CancellationToken cancellationToken)
    {
        var existing = await transactionRepository.FindByPaymentScheduleIdAsync(
            request.PaymentScheduleId,
            cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        var schedule = await paymentScheduleRepository.FindByIdAsync(request.PaymentScheduleId, cancellationToken);
        if (schedule is null)
        {
            throw new ArgumentException(
                $"Payment schedule '{request.PaymentScheduleId}' was not found.",
                nameof(request));
        }

        var transaction = Domain.Entities.Transaction.CreatePending(
            request.Amount,
            request.PaymentScheduleId,
            request.ContactId,
            request.PaymentMethodId,
            request.PaymentType,
            schedule.BookDate,
            request.IsRecurring);

        await transactionRepository.AddAsync(transaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction.Id;
    }
}
