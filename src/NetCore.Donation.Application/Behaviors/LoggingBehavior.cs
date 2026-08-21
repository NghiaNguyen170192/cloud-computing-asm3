using MediatR;
using Microsoft.Extensions.Logging;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Behaviors;

/// <summary>
/// Pipeline behavior for logging requests and responses.
/// Includes correlation ID for end-to-end request tracing.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> logger;
    private readonly ICorrelationIdAccessor? correlationIdAccessor;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICorrelationIdAccessor? correlationIdAccessor = null)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.correlationIdAccessor = correlationIdAccessor;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var correlationId = correlationIdAccessor?.CorrelationId ?? Guid.NewGuid().ToString("N");

        logger.LogInformation(
            "Handling {RequestName} with Correlation ID {CorrelationId}",
            requestName,
            correlationId);

        try
        {
            var response = await next(cancellationToken);
            logger.LogInformation(
                "Handled {RequestName} successfully (Correlation ID: {CorrelationId})",
                requestName,
                correlationId);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error handling {RequestName} (Correlation ID: {CorrelationId})",
                requestName,
                correlationId);
            throw;
        }
    }
}
