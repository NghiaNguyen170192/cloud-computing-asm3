using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Application.Tests;

/// <summary>
/// Simple in-memory mock implementation of ICacheRepository for testing purposes.
/// Tracks method call counts to verify cache operations.
/// </summary>
/// <typeparam name="T">The type of entity to cache.</typeparam>
public class MockCacheRepository<T> : ICacheRepository<T> where T : class
{
    private readonly Dictionary<string, T> cache = new();

    public int AddAsyncSingleCallCount { get; private set; }

    public int AddAsyncBulkCallCount { get; private set; }

    public int DeleteAsyncCallCount { get; private set; }

    public int FindByIdAsyncCallCount { get; private set; }

    public int UpdateAsyncCallCount { get; private set; }

    public Task<string> AddAsync(T item)
    {
        AddAsyncSingleCallCount++;
        var key = Guid.NewGuid().ToString();
        cache[key] = item;
        return Task.FromResult(key);
    }

    public Task<IEnumerable<string>> AddAsync(IEnumerable<T> items)
    {
        AddAsyncBulkCallCount++;
        var keys = new List<string>();
        foreach (var item in items)
        {
            var key = Guid.NewGuid().ToString();
            cache[key] = item;
            keys.Add(key);
        }
        return Task.FromResult<IEnumerable<string>>(keys);
    }

    public Task DeleteAsync(T item)
    {
        DeleteAsyncCallCount++;
        var entry = cache.FirstOrDefault(x => x.Value == item);
        if (!entry.Equals(default(KeyValuePair<string, T>)))
        {
            cache.Remove(entry.Key);
        }
        return Task.CompletedTask;
    }

    public Task<T?> FindByIdAsync(string id)
    {
        FindByIdAsyncCallCount++;
        cache.TryGetValue(id, out var item);
        return Task.FromResult(item);
    }

    public Task UpdateAsync(T item)
    {
        UpdateAsyncCallCount++;
        var entry = cache.FirstOrDefault(x => x.Value == item);
        if (!entry.Equals(default(KeyValuePair<string, T>)))
        {
            cache[entry.Key] = item;
        }

        return Task.CompletedTask;
    }
}