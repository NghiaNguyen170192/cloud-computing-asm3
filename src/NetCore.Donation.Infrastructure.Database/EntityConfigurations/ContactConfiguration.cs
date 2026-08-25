using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetCore.Donation.Domain.Entities;

namespace NetCore.Donation.Infrastructure.Database.EntityConfigurations;

public class ContactConfiguration : EntityTypeConfiguration<Contact>
{
    public override void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.Property(contact => contact.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(contact => contact.LastName).IsRequired().HasMaxLength(100);
        builder.Property(contact => contact.AddressLine).IsRequired().HasMaxLength(500);
        builder.Property(contact => contact.Email).IsRequired().HasMaxLength(320);
        builder.Property(contact => contact.PhoneNumber).IsRequired().HasMaxLength(32);
        builder.Property(contact => contact.Gender).HasConversion<string>().HasMaxLength(20);
        builder.Property(contact => contact.DoNotEmail).IsRequired().HasDefaultValue(false);
        builder.Property(contact => contact.DoNotSms).IsRequired().HasDefaultValue(false);
        builder.HasIndex(contact => contact.Email);
        builder.HasIndex(contact => contact.FirstName);
        builder.HasIndex(contact => contact.LastName);
        builder.HasIndex(contact => contact.PhoneNumber);
        builder.HasIndex(contact => contact.Gender);
        builder.HasIndex(contact => contact.CountryId);

        builder
            .HasOne(contact => contact.Country)
            .WithMany()
            .HasForeignKey(contact => contact.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}