namespace NetCore.Donation.Domain.Entities;

/// <summary>
/// Represents a log entry for idempotent request handling.
/// Used to track and replay responses for duplicate requests using the same correlation ID.
/// </summary>
public class IdempotencyLog
{
    public Guid Id { get; set; }

    /// <summary>
    /// Unique correlation ID for the request.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// The type of command/query that was executed (e.g., "CreateCountriesCommand").
    /// </summary>
    public string RequestType { get; set; } = string.Empty;

    /// <summary>
    /// The HTTP method (POST, PUT, DELETE, GET).
    /// </summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>
    /// The request path/endpoint.
    /// </summary>
    public string RequestPath { get; set; } = string.Empty;

    /// <summary>
    /// The serialized response data (JSON).
    /// </summary>
    public string ResponseData { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code of the response.
    /// </summary>
    public int ResponseStatusCode { get; set; }

    /// <summary>
    /// Timestamp when the original request was processed.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp for expiration/cleanup. Allows removing old idempotency records.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Indicates if this has been explicitly marked for cleanup.
    /// </summary>
    public bool IsExpired { get; set; }
}
