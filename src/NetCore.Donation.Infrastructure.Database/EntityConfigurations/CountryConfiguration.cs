using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetCore.Donation.Domain.Entities;

namespace NetCore.Donation.Infrastructure.Database.EntityConfigurations;

public class CountryConfiguration : EntityTypeConfiguration<Country>
{
    public override void Configure(EntityTypeBuilder<Country> builder)
    {
        // builder.Ignore(b => b.DomainEvents);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(256);
        builder.Property(c => c.CountryCode).IsRequired().HasMaxLength(3);
        builder.Property(c => c.Alpha2).IsRequired().HasMaxLength(2);
        builder.Property(c => c.Alpha3).IsRequired().HasMaxLength(3);
    }
}