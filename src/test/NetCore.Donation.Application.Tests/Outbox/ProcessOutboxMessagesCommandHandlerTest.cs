using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetCore.Donation.Application.Outbox.Process;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.Events;
using NetCore.Donation.Infrastructure.Database;
using NetCore.Donation.Infrastructure.Database.Messaging;

namespace NetCore.Donation.Application.Tests.Outbox;

[TestClass]
public class ProcessOutboxMessagesCommandHandlerTest : BaseTest
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [TestMethod]
    public async Task Handle_WhenPending_PublishesStubAndInProcessHandlerThenMarksProcessed()
    {
        var handled = 0;
        TestCountryCreatedHandler.SetHandler(() => handled++);

        var (context, handler, publisher) = CreateHarness();
        await using (context)
        {
            context.OutboxMessages.Add(CreatePendingCountryCreatedMessage("corr-1", "idem-1"));
            await context.SaveChangesAsync(CancellationToken.None);

            var processed = await handler.Handle(new ProcessOutboxMessagesCommand(), CancellationToken.None);

            Assert.AreEqual(1, processed);
            Assert.AreEqual(1, handled);
            Assert.HasCount(1, publisher.Published);
            Assert.AreEqual("corr-1", publisher.Published[0].CorrelationId);
            Assert.AreEqual("idem-1", publisher.Published[0].IdempotencyKey);

            var stored = await context.OutboxMessages.SingleAsync();
            Assert.IsNotNull(stored.ProcessedAtUtc);
            Assert.IsNull(stored.LastError);
        }
    }

    [TestMethod]
    public async Task Handle_WhenPublisherThrows_LeavesPendingIncrementsAttemptCountThenSucceedsOnRetry()
    {
        var handled = 0;
        TestCountryCreatedHandler.SetHandler(() => handled++);

        var (context, handler, publisher) = CreateHarness();
        await using (context)
        {
            context.OutboxMessages.Add(CreatePendingCountryCreatedMessage("corr-2", "idem-2"));
            await context.SaveChangesAsync(CancellationToken.None);

            publisher.ShouldThrow = true;
            var first = await handler.Handle(new ProcessOutboxMessagesCommand(), CancellationToken.None);

            Assert.AreEqual(0, first);
            Assert.AreEqual(0, handled);
            var failed = await context.OutboxMessages.SingleAsync();
            Assert.IsNull(failed.ProcessedAtUtc);
            Assert.AreEqual(1, failed.AttemptCount);
            Assert.IsFalse(string.IsNullOrWhiteSpace(failed.LastError));

            publisher.ShouldThrow = false;
            var second = await handler.Handle(new ProcessOutboxMessagesCommand(), CancellationToken.None);

            Assert.AreEqual(1, second);
            Assert.AreEqual(1, handled);
            var stored = await context.OutboxMessages.SingleAsync();
            Assert.IsNotNull(stored.ProcessedAtUtc);
            Assert.AreEqual(1, stored.AttemptCount);
        }
    }

    private static (ApplicationDatabaseContext Context, ProcessOutboxMessagesCommandHandler Handler, RecordingIntegrationEventPublisher Publisher) CreateHarness()
    {
        var options = new DbContextOptionsBuilder<ApplicationDatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ProcessOutboxMessagesCommandHandlerTest>());
        services.AddScoped<INotificationHandler<CountryCreatedDomainEvent>, TestCountryCreatedHandler>();
        var serviceProvider = services.BuildServiceProvider();

        var context = new ApplicationDatabaseContext(options, serviceProvider.GetRequiredService<IPublisher>());
        context.Database.EnsureCreated();

        var publisher = new RecordingIntegrationEventPublisher();
        var handler = new ProcessOutboxMessagesCommandHandler(
            new OutboxMessageRepository(context),
            publisher,
            serviceProvider.GetRequiredService<IPublisher>(),
            context);

        return (context, handler, publisher);
    }

    private static OutboxMessage CreatePendingCountryCreatedMessage(string correlationId, string idempotencyKey)
    {
        var domainEvent = new CountryCreatedDomainEvent(Guid.NewGuid(), "Test");
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions);
        return OutboxMessage.Create(
            domainEvent.GetType().AssemblyQualifiedName!,
            payload,
            correlationId,
            idempotencyKey);
    }

    private sealed class TestCountryCreatedHandler : INotificationHandler<CountryCreatedDomainEvent>
    {
        private static Action? onHandle;

        public static void SetHandler(Action handler) => onHandle = handler;

        public Task Handle(CountryCreatedDomainEvent notification, CancellationToken cancellationToken)
        {
            onHandle?.Invoke();
            return Task.CompletedTask;
        }
    }
}
