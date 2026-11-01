using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentsDemo.Api.Features.Cards.Infrastructure.Domain.Models;

namespace PaymentsDemo.Api.Features.Cards.Infrastructure.Domain.EntityConfigurations;

public class BankCardEntityConfiguration : IEntityTypeConfiguration<BankCard>
{
    public void Configure(EntityTypeBuilder<BankCard> builder)
    {
        builder.ToTable("BankCards", "payments", t =>
        {
            t.HasCheckConstraint("CK_BankCards_ExpiryMonth", "\"ExpiryOnMonth\" BETWEEN 1 AND 12");
        });

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.CustomerId).IsRequired().HasMaxLength(50);
        builder.Property(c => c.CardToken).IsRequired().HasMaxLength(64);

        builder.Property(c => c.CardNumber).IsRequired();
        builder.Property(c => c.NameOnCard).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Cvc).IsRequired().HasMaxLength(4);


        builder.Property(c => c.ExpiresOnDay).IsRequired();
        builder.Property(c => c.ExpiresOnMonth).IsRequired();
        builder.Property(c => c.ExpiresOnYear).IsRequired();
        builder.Property(c => c.IsActive).IsRequired();
        builder.Property(c => c.CreatedOnUTC).IsRequired();
        builder.Property(c => c.CreatedBy).IsRequired();

        // Uniqueness
        builder.HasIndex(c => c.CardToken)
            .IsUnique()
            .HasDatabaseName("UX_BankCards_CardToken");

        // Lookup used at payment time: a customer's active cards
        builder.HasIndex(c => new { c.CustomerId, c.IsActive })
            .HasDatabaseName("IX_BankCards_Customer_Active");
    }
}