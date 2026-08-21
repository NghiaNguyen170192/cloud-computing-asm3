namespace NetCore.Donation.Domain.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
