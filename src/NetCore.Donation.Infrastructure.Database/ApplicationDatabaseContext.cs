#nullable enable
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.SharedKernel;
using NetCore.Donation.Infrastructure.Database.Extensions;

namespace NetCore.Donation.Infrastructure.Database;

#pragma warning disable CS9113 // IPublisher is retained so existing tests can pass it as the second constructor argument.
public class ApplicationDatabaseContext(
    DbContextOptions<ApplicationDatabaseContext> databaseContextOptions,
    IPublisher? publisher = null,
    ICorrelationIdAccessor? correlationIdAccessor = null,
    IIdempotencyKeyAccessor? idempotencyKeyAccessor = null)
    : DbContext(databaseContextOptions), IUnitOfWork
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public DbSet<Country> Countries { get; set; }

    public DbSet<Contact> Contacts { get; set; }

    public DbSet<PaymentMethod> PaymentMethods { get; set; }

    public DbSet<Receipt> Receipts { get; set; }

    public DbSet<PaymentSchedule> PaymentSchedules { get; set; }

    public DbSet<Transaction> Transactions { get; set; }

    public DbSet<Journal> Journals { get; set; }

    public DbSet<IdempotencyLog> IdempotencyLogs { get; set; }

    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
    {
        modelBuilder.SetDefaultValueTableName();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDatabaseContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        AuditableSaveChanges();
        CaptureDomainEventsAsOutboxMessages();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        AuditableSaveChanges();
        CaptureDomainEventsAsOutboxMessages();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void AuditableSaveChanges()
    {
        var entries = ChangeTracker
            .Entries<Entity>()
            .Where(entity => entity.State is not EntityState.Unchanged);

        foreach (var entry in entries)
        {
            AppendAuditableProperties(entry);
        }
    }

    private static void AppendAuditableProperties(EntityEntry<Entity> entry)
    {
        entry.Entity.ModifiedDate = DateTime.UtcNow;
        entry.Entity.ModifiedBy = Guid.Empty;

        if (entry.State != EntityState.Added)
        {
            return;
        }

        if (entry.Entity.Id == Guid.Empty)
        {
            entry.Entity.Id = Guid.NewGuid();
        }

        entry.Entity.CreatedDate = DateTime.UtcNow;
        entry.Entity.CreatedBy = Guid.Empty;
    }

    private void CaptureDomainEventsAsOutboxMessages()
    {
        var correlationId = correlationIdAccessor?.CorrelationId ?? Guid.NewGuid().ToString("N");
        var idempotencyKey = idempotencyKeyAccessor?.IdempotencyKey;
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            idempotencyKey = correlationId;
        }

        var domainEntities = ChangeTracker
            .Entries<Entity>()
            .Where(entry => entry.Entity.DomainEvents.Any())
            .ToList();

        foreach (var entry in domainEntities)
        {
            if (entry.State is EntityState.Deleted or EntityState.Detached)
            {
                entry.Entity.ClearDomainEvents();
                continue;
            }

            foreach (var domainEvent in entry.Entity.DomainEvents)
            {
                var messageType = domainEvent.GetType().AssemblyQualifiedName
                    ?? domainEvent.GetType().FullName
                    ?? domainEvent.GetType().Name;

                if (HasOutboxMessage(idempotencyKey, messageType))
                {
                    continue;
                }

                var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions);
                OutboxMessages.Add(OutboxMessage.Create(messageType, payload, correlationId, idempotencyKey));
            }

            entry.Entity.ClearDomainEvents();
        }
    }

    private bool HasOutboxMessage(string idempotencyKey, string messageType)
    {
        if (OutboxMessages.Local.Any(message =>
            message.IdempotencyKey == idempotencyKey && message.MessageType == messageType))
        {
            return true;
        }

        return OutboxMessages.Any(message =>
            message.IdempotencyKey == idempotencyKey && message.MessageType == messageType);
    }
}
#pragma warning restore CS9113
