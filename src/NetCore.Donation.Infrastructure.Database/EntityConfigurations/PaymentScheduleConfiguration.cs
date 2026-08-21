using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Infrastructure.Database.EntityConfigurations;

public class PaymentScheduleConfiguration : EntityTypeConfiguration<PaymentSchedule>
{
    public override void Configure(EntityTypeBuilder<PaymentSchedule> builder)
    {
        builder.Property(schedule => schedule.Amount).HasPrecision(18, 2);
        builder.Property(schedule => schedule.PaymentType).HasConversion<string>().HasMaxLength(20);
        builder.Property(schedule => schedule.RecurringInterval).HasConversion<string>().HasMaxLength(20);
        builder.Property(schedule => schedule.Identifier).IsRequired().HasMaxLength(RecordIdentifier.MaxLength);
        builder.HasIndex(schedule => schedule.Identifier).IsUnique();
        builder.HasIndex(schedule => schedule.ContactId);
        builder.HasIndex(schedule => schedule.PaymentMethodId);

        builder
            .HasOne(schedule => schedule.Contact)
            .WithMany()
            .HasForeignKey(schedule => schedule.ContactId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(schedule => schedule.PaymentMethod)
            .WithMany()
            .HasForeignKey(schedule => schedule.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}