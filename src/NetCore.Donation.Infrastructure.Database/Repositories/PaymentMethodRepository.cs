#nullable enable

using Microsoft.EntityFrameworkCore;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Infrastructure.Database.Repositories;

public class PaymentMethodRepository(ApplicationDatabaseContext applicationDatabaseContext) : IPaymentMethodRepository
{
    public async Task AddAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken)
    {
        await applicationDatabaseContext.PaymentMethods.AddAsync(paymentMethod, cancellationToken);
    }

    public async Task<bool> IsExistAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await applicationDatabaseContext.PaymentMethods
            .AsNoTracking()
            .AnyAsync(paymentMethod => paymentMethod.Id == id, cancellationToken);

        return result;
    }

    public async Task<PaymentMethod?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await applicationDatabaseContext.PaymentMethods.FindAsync([id], cancellationToken);
    }

    public void Delete(PaymentMethod paymentMethod)
    {
        applicationDatabaseContext.PaymentMethods.Remove(paymentMethod);
    }

    public IQueryable<PaymentMethod> GetAll()
    {
        return applicationDatabaseContext.PaymentMethods.AsNoTracking();
    }
}