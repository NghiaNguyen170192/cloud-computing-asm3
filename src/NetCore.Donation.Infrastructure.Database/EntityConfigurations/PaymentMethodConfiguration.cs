using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetCore.Donation.Domain.Entities;

namespace NetCore.Donation.Infrastructure.Database.EntityConfigurations;

public class PaymentMethodConfiguration : EntityTypeConfiguration<PaymentMethod>
{
    public override void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.Property(method => method.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(method => method.PaymentType).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(method => method.ContactId);

        builder
            .HasOne(method => method.Contact)
            .WithMany()
            .HasForeignKey(method => method.ContactId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}