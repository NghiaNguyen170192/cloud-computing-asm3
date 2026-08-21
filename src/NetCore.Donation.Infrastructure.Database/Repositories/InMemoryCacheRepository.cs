using System.Collections.Concurrent;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Infrastructure.Database.Repositories;

/// <summary>
/// Simple in-memory cache repository used as a fallback when Redis is not available
/// or when an entity type is not decorated for Redis. Intended for local/dev usage.
/// </summary>
public class InMemoryCacheRepository<T> : ICacheRepository<T> where T : class
{
    private readonly ConcurrentDictionary<string, T> cache = new();

    public Task<string> AddAsync(T item)
    {
        var key = Guid.NewGuid().ToString();
        cache[key] = item;
        return Task.FromResult(key);
    }

    public Task<IEnumerable<string>> AddAsync(IEnumerable<T> items)
    {
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
        var entry = cache.FirstOrDefault(kv => ReferenceEquals(kv.Value, item));
        if (!entry.Equals(default(KeyValuePair<string, T>)))
        {
            cache.TryRemove(entry.Key, out _);
        }

        return Task.CompletedTask;
    }

    public Task<T?> FindByIdAsync(string id)
    {
        cache.TryGetValue(id, out var item);
        return Task.FromResult(item);
    }

    public Task UpdateAsync(T item)
    {
        var entry = cache.FirstOrDefault(kv => ReferenceEquals(kv.Value, item));
        if (!entry.Equals(default(KeyValuePair<string, T>)))
        {
            cache[entry.Key] = item;
        }

        return Task.CompletedTask;
    }
}
