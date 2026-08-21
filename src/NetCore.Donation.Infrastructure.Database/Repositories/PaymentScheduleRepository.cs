#nullable enable

using Microsoft.EntityFrameworkCore;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Infrastructure.Database.Repositories;

public class PaymentScheduleRepository(ApplicationDatabaseContext applicationDatabaseContext)
    : IPaymentScheduleRepository
{
    public async Task AddAsync(PaymentSchedule paymentSchedule, CancellationToken cancellationToken)
    {
        await applicationDatabaseContext.PaymentSchedules.AddAsync(paymentSchedule, cancellationToken);
    }

    public async Task<bool> IsExistAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await applicationDatabaseContext.PaymentSchedules
            .AsNoTracking()
            .AnyAsync(paymentSchedule => paymentSchedule.Id == id, cancellationToken);

        return result;
    }

    public async Task<PaymentSchedule?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await applicationDatabaseContext.PaymentSchedules.FindAsync([id], cancellationToken);
    }

    public void Delete(PaymentSchedule paymentSchedule)
    {
        applicationDatabaseContext.PaymentSchedules.Remove(paymentSchedule);
    }

    public IQueryable<PaymentSchedule> GetAll()
    {
        return applicationDatabaseContext.PaymentSchedules.AsNoTracking();
    }
}