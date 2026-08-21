using Microsoft.EntityFrameworkCore.Diagnostics;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.Events;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Infrastructure.Database.Tests;

[TestClass]
public class OutboxCaptureTests
{
    [TestMethod]
    public async Task SaveChangesAsync_CapturesDomainEventAsOutboxMessageWithCorrelationId()
    {
        var correlationId = "correlation-trace-1";
        var idempotencyKey = "idempotency-trace-1";
        var options = new DbContextOptionsBuilder<ApplicationDatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDatabaseContext(
            options,
            publisher: null,
            new TestCorrelationIdAccessor(correlationId),
            new TestIdempotencyKeyAccessor(idempotencyKey));
        await context.Database.EnsureCreatedAsync();

        context.Countries.Add(Country.Create("Australia", "036", "AU", "AUS"));
        await context.SaveChangesAsync(CancellationToken.None);

        var messages = await context.OutboxMessages.ToListAsync();
        Assert.HasCount(1, messages);
        Assert.AreEqual(correlationId, messages[0].CorrelationId);
        Assert.AreEqual(idempotencyKey, messages[0].IdempotencyKey);
        Assert.IsNull(messages[0].ProcessedAtUtc);
        StringAssert.Contains(messages[0].MessageType, nameof(CountryCreatedDomainEvent));
    }

    [TestMethod]
    public async Task SaveChangesAsync_WhenSaveFails_DoesNotPersistOutboxMessage()
    {
        var databaseName = Guid.NewGuid().ToString();
        var failingOptions = new DbContextOptionsBuilder<ApplicationDatabaseContext>()
            .UseInMemoryDatabase(databaseName)
            .AddInterceptors(new ThrowOnSaveInterceptor())
            .Options;

        await using (var context = new ApplicationDatabaseContext(failingOptions))
        {
            await context.Database.EnsureCreatedAsync();
            context.Countries.Add(Country.Create("Australia", "036", "AU", "AUS"));

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => context.SaveChangesAsync(CancellationToken.None));
        }

        var readOptions = new DbContextOptionsBuilder<ApplicationDatabaseContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        await using var readContext = new ApplicationDatabaseContext(readOptions);
        Assert.AreEqual(0, await readContext.OutboxMessages.CountAsync());
        Assert.AreEqual(0, await readContext.Countries.CountAsync());
    }

    [TestMethod]
    public async Task SaveChangesAsync_SameIdempotencyKeyAndMessageType_DoesNotEnqueueSecondPendingMessage()
    {
        var options = new DbContextOptionsBuilder<ApplicationDatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDatabaseContext(
            options,
            publisher: null,
            new TestCorrelationIdAccessor("correlation-shared"),
            new TestIdempotencyKeyAccessor("idempotency-shared"));
        await context.Database.EnsureCreatedAsync();

        context.Countries.Add(Country.Create("Australia", "036", "AU", "AUS"));
        context.Countries.Add(Country.Create("New Zealand", "554", "NZ", "NZL"));
        await context.SaveChangesAsync(CancellationToken.None);

        var pending = await context.OutboxMessages
            .Where(message => message.ProcessedAtUtc == null)
            .ToListAsync();

        Assert.HasCount(1, pending);
        Assert.AreEqual(2, await context.Countries.CountAsync());
    }

    [TestMethod]
    public async Task SaveChangesAsync_WhenIdempotencyKeyMissing_CopiesCorrelationId()
    {
        var correlationId = "correlation-only";
        var options = new DbContextOptionsBuilder<ApplicationDatabaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDatabaseContext(
            options,
            publisher: null,
            new TestCorrelationIdAccessor(correlationId),
            new TestIdempotencyKeyAccessor(null));
        await context.Database.EnsureCreatedAsync();

        context.Countries.Add(Country.Create("Australia", "036", "AU", "AUS"));
        await context.SaveChangesAsync(CancellationToken.None);

        var message = await context.OutboxMessages.SingleAsync();
        Assert.AreEqual(correlationId, message.CorrelationId);
        Assert.AreEqual(correlationId, message.IdempotencyKey);
    }

    private sealed class TestCorrelationIdAccessor(string correlationId) : ICorrelationIdAccessor
    {
        public string CorrelationId { get; } = correlationId;
    }

    private sealed class TestIdempotencyKeyAccessor(string? idempotencyKey) : IIdempotencyKeyAccessor
    {
        public string? IdempotencyKey { get; } = idempotencyKey;
    }

    private sealed class ThrowOnSaveInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            throw new InvalidOperationException("Save failed.");
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Save failed.");
        }
    }
}
