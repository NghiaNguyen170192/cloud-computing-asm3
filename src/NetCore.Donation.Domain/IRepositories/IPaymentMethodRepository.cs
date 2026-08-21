using NetCore.Donation.Domain.Entities;

namespace NetCore.Donation.Domain.IRepositories;

public interface IPaymentMethodRepository
{
    Task AddAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken);

    Task<bool> IsExistAsync(Guid id, CancellationToken cancellationToken);

    Task<PaymentMethod?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    void Delete(PaymentMethod paymentMethod);

    IQueryable<PaymentMethod> GetAll();
}