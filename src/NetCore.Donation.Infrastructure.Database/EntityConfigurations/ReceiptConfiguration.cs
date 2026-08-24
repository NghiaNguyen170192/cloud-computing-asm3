using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Infrastructure.Database.EntityConfigurations;

public class ReceiptConfiguration : EntityTypeConfiguration<Receipt>
{
    public override void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.Property(receipt => receipt.Identifier).IsRequired().HasMaxLength(RecordIdentifier.MaxLength);
        builder.HasIndex(receipt => receipt.Identifier).IsUnique();
        builder.HasIndex(receipt => receipt.ContactId);
        builder.HasIndex(receipt => receipt.TransactionId);
        builder.HasIndex(receipt => receipt.PaymentScheduleId);
        builder.HasIndex(receipt => receipt.DocumentObjectKey);

        builder.Property(receipt => receipt.DocumentObjectKey).HasMaxLength(512);
        builder.Property(receipt => receipt.DocumentFileName).HasMaxLength(256);
        builder.Property(receipt => receipt.DocumentContentType).HasMaxLength(128);

        builder
            .HasOne(receipt => receipt.Contact)
            .WithMany()
            .HasForeignKey(receipt => receipt.ContactId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(receipt => receipt.Transaction)
            .WithMany(transaction => transaction.Receipts)
            .HasForeignKey(receipt => receipt.TransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(receipt => receipt.PaymentSchedule)
            .WithMany()
            .HasForeignKey(receipt => receipt.PaymentScheduleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
