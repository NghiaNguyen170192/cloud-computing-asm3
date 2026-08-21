#nullable enable
using System.Collections.Concurrent;
using NetCore.Donation.Domain.Messaging;

namespace NetCore.Donation.Infrastructure.Database.Messaging;

public class RecordingIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly ConcurrentQueue<IntegrationEvent> published = new();

    public bool ShouldThrow { get; set; }

    public string? ThrowMessage { get; set; }

    public IReadOnlyList<IntegrationEvent> Published => published.ToList();

    public Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        if (ShouldThrow)
        {
            throw new InvalidOperationException(ThrowMessage ?? "Integration event publisher failed.");
        }

        published.Enqueue(integrationEvent);
        return Task.CompletedTask;
    }

    public void Clear()
    {
        published.Clear();
    }
}
