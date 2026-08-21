namespace NetCore.Donation.Domain.SharedKernel;

public interface IUnitOfWork
{
	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}