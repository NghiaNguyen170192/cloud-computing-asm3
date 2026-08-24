using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetCore.Donation.Domain.Entities;

namespace NetCore.Donation.Infrastructure.Database.EntityConfigurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.MessageType)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(message => message.Payload)
            .IsRequired();

        builder.Property(message => message.CorrelationId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(message => message.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(message => message.OccurredAtUtc)
            .IsRequired();

        builder.Property(message => message.AttemptCount)
            .IsRequired();

        builder.Property(message => message.LastError)
            .HasMaxLength(2000);

        builder.HasIndex(message => new { message.ProcessedAtUtc, message.OccurredAtUtc })
            .HasDatabaseName("IX_OutboxMessages_Pending");

        builder.HasIndex(message => message.CorrelationId)
            .HasDatabaseName("IX_OutboxMessages_CorrelationId");

        builder.HasIndex(message => message.IdempotencyKey)
            .HasDatabaseName("IX_OutboxMessages_IdempotencyKey");
    }
}
