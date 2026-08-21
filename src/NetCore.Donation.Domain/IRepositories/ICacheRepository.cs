namespace NetCore.Donation.Domain.IRepositories;

public interface ICacheRepository<T>
	where T : class
{
	Task<string> AddAsync(T item);

	Task<IEnumerable<string>> AddAsync(IEnumerable<T> items);

	Task DeleteAsync(T item);

	Task<T?> FindByIdAsync(string id);

	Task UpdateAsync(T item);
}