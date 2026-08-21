#nullable enable
using MediatR;
using Microsoft.Extensions.Logging;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;
using System.Text.Json;

namespace NetCore.Donation.Application.Behaviors;

/// <summary>
/// Pipeline behavior for handling idempotent command execution.
/// Checks if a request with the same correlation ID has been processed before,
/// and if so, returns the cached response instead of re-executing the command.
/// </summary>
/// <typeparam name="TRequest">The request type (typically a command).</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int DefaultIdempotencyTtlMinutes = 24 * 60; // 24 hours

    private readonly IIdempotencyRepository? idempotencyRepository;
    private readonly ICorrelationIdAccessor? correlationIdAccessor;
    private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>> logger;

    public IdempotencyBehavior(
        ILogger<IdempotencyBehavior<TRequest, TResponse>> logger,
        IIdempotencyRepository? idempotencyRepository = null,
        ICorrelationIdAccessor? correlationIdAccessor = null)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.idempotencyRepository = idempotencyRepository;
        this.correlationIdAccessor = correlationIdAccessor;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Skip idempotency checks if repository or accessor not available
        if (idempotencyRepository == null || correlationIdAccessor == null)
        {
            return await next(cancellationToken);
        }

        var requestType = typeof(TRequest).Name;
        var correlationId = correlationIdAccessor.CorrelationId;

        try
        {
            // Check if we've already processed this request
            var existingLog = await idempotencyRepository.GetByCorrelationIdAsync(
                correlationId,
                requestType,
                cancellationToken);

            if (existingLog != null)
            {
                logger.LogInformation(
                    "Found existing idempotency log for {RequestType} with correlation ID {CorrelationId}. Returning cached response.",
                    requestType,
                    correlationId);

                // Deserialize and return the cached response
                return DeserializeResponse(existingLog.ResponseData);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Error checking idempotency for {RequestType} with correlation ID {CorrelationId}. Proceeding with normal execution.",
                requestType,
                correlationId);
        }

        // Execute the handler
        var response = await next(cancellationToken);

        // Store the result for future idempotent calls
        try
        {
            await StoreIdempotencyLogAsync(
                requestType,
                correlationId,
                response,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Error storing idempotency log for {RequestType} with correlation ID {CorrelationId}.",
                requestType,
                correlationId);
        }

        return response;
    }

    private async Task StoreIdempotencyLogAsync(
        string requestType,
        string correlationId,
        TResponse response,
        CancellationToken cancellationToken)
    {
        var idempotencyLog = new Domain.Entities.IdempotencyLog
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            RequestType = requestType,
            HttpMethod = "POST",
            RequestPath = requestType,
            ResponseData = SerializeResponse(response),
            ResponseStatusCode = 200,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(DefaultIdempotencyTtlMinutes),
            IsExpired = false,
        };

        await idempotencyRepository!.AddAsync(idempotencyLog, cancellationToken);
        await idempotencyRepository.SaveAsync(cancellationToken);

        logger.LogInformation(
            "Stored idempotency log for {RequestType} with correlation ID {CorrelationId}.",
            requestType,
            correlationId);
    }

    private static string SerializeResponse(TResponse response)
    {
        try
        {
            return JsonSerializer.Serialize(response);
        }
        catch
        {
            // Fallback for non-serializable responses
            return string.Empty;
        }
    }

    private static TResponse DeserializeResponse(string responseData)
    {
        try
        {
            return JsonSerializer.Deserialize<TResponse>(responseData)
                ?? throw new InvalidOperationException("Failed to deserialize cached response.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to deserialize cached idempotency response.", ex);
        }
    }
}