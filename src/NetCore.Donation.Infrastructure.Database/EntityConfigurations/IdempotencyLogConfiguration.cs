using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetCore.Donation.Domain.Entities;

namespace NetCore.Donation.Infrastructure.Database.EntityConfigurations;

/// <summary>
/// Entity configuration for IdempotencyLog.
/// </summary>
public class IdempotencyLogConfiguration : IEntityTypeConfiguration<IdempotencyLog>
{
    public void Configure(EntityTypeBuilder<IdempotencyLog> builder)
    {
        builder.ToTable("IdempotencyLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CorrelationId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.RequestType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.HttpMethod)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.RequestPath)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.ResponseData)
            .IsRequired();

        builder.Property(x => x.ResponseStatusCode)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.IsExpired)
            .IsRequired()
            .HasDefaultValue(false);

        // Create unique index on correlation ID and request type to prevent duplicates
        builder.HasIndex(x => new { x.CorrelationId, x.RequestType })
            .IsUnique()
            .HasDatabaseName("IX_IdempotencyLog_CorrelationId_RequestType");

        // Index for cleanup queries
        builder.HasIndex(x => new { x.ExpiresAt, x.IsExpired })
            .HasDatabaseName("IX_IdempotencyLog_Expiry");
    }
}