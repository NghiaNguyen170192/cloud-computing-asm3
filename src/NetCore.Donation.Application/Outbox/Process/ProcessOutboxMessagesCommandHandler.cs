using System.Text.Json;
using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.Messaging;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Outbox.Process;

public class ProcessOutboxMessagesCommandHandler(
    IOutboxMessageRepository outboxMessageRepository,
    IIntegrationEventPublisher integrationEventPublisher,
    IPublisher publisher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ProcessOutboxMessagesCommand, int>
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<int> Handle(ProcessOutboxMessagesCommand request, CancellationToken cancellationToken)
    {
        var pending = await outboxMessageRepository.GetPendingAsync(request.BatchSize, cancellationToken);
        var processed = 0;

        foreach (var message in pending)
        {
            try
            {
                var notification = Deserialize(message.MessageType, message.Payload);
                await integrationEventPublisher.PublishAsync(
                    new IntegrationEvent(
                        message.MessageType,
                        message.Payload,
                        message.CorrelationId,
                        message.IdempotencyKey),
                    cancellationToken);

                if (notification is not null)
                {
                    await publisher.Publish(notification, cancellationToken);
                }

                message.MarkProcessed();
                processed++;
            }
            catch (Exception exception)
            {
                message.RecordFailure(exception.Message);
            }
        }

        if (pending.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return processed;
    }

    private static INotification? Deserialize(string messageType, string payload)
    {
        var type = Type.GetType(messageType);
        if (type is null)
        {
            throw new InvalidOperationException($"Unknown outbox message type '{messageType}'.");
        }

        var notification = JsonSerializer.Deserialize(payload, type, SerializerOptions) as INotification;
        if (notification is null)
        {
            throw new InvalidOperationException($"Outbox payload could not be deserialized as '{messageType}'.");
        }

        return notification;
    }
}
