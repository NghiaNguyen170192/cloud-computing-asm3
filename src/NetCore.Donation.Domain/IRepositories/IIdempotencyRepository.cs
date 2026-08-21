using NetCore.Donation.Domain.Entities;

namespace NetCore.Donation.Domain.IRepositories;

/// <summary>
/// Repository for managing idempotency logs.
/// </summary>
public interface IIdempotencyRepository
{
    /// <summary>
    /// Gets an idempotency log entry by correlation ID and request type.
    /// </summary>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="requestType">The type of the request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The idempotency log entry if found, otherwise null.</returns>
    Task<IdempotencyLog?> GetByCorrelationIdAsync(string correlationId, string requestType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new idempotency log entry.
    /// </summary>
    /// <param name="idempotencyLog">The idempotency log entry to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AddAsync(IdempotencyLog idempotencyLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the repository.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired idempotency logs.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of deleted records.</returns>
    Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default);
}
