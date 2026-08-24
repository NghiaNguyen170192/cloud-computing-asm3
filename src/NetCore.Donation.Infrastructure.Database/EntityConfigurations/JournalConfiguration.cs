using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Infrastructure.Database.EntityConfigurations;

public class JournalConfiguration : EntityTypeConfiguration<Journal>
{
    public override void Configure(EntityTypeBuilder<Journal> builder)
    {
        builder.Property(journal => journal.Identifier).IsRequired().HasMaxLength(RecordIdentifier.MaxLength);
        builder.HasIndex(journal => journal.Identifier).IsUnique();
        builder.HasIndex(journal => journal.TransactionId);

        builder
            .HasOne(journal => journal.Transaction)
            .WithMany(transaction => transaction.Journals)
            .HasForeignKey(journal => journal.TransactionId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
