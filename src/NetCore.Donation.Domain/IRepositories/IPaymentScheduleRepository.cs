using NetCore.Donation.Domain.Entities;

namespace NetCore.Donation.Domain.IRepositories;

public interface IPaymentScheduleRepository
{
    Task AddAsync(PaymentSchedule paymentSchedule, CancellationToken cancellationToken);

    Task<bool> IsExistAsync(Guid id, CancellationToken cancellationToken);

    Task<PaymentSchedule?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    void Delete(PaymentSchedule paymentSchedule);

    IQueryable<PaymentSchedule> GetAll();
}