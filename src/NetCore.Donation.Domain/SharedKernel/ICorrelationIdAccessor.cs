namespace NetCore.Donation.Domain.SharedKernel;

/// <summary>
/// Provides access to the current correlation ID for the request.
/// Used to track requests across API, application, and infrastructure layers.
/// </summary>
public interface ICorrelationIdAccessor
{
    /// <summary>
    /// Gets the current correlation ID.
    /// </summary>
    string CorrelationId { get; }
}
