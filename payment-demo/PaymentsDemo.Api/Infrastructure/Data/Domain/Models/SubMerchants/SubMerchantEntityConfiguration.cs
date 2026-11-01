using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentsDemo.Api.Infrastructure.Data.Domain.Models.ValueObjects;

namespace PaymentsDemo.Api.Infrastructure.Data.Domain.Models.SubMerchants;

public class SubMerchantEntityConfiguration : IEntityTypeConfiguration<SubMerchant>
{
    public void Configure(EntityTypeBuilder<SubMerchant> builder)
    {
        builder.ToTable("SubMerchants", "payments");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.MerchantId).IsRequired();

        builder.Property(s => s.Code)
            .HasConversion(c => c.Value, v => SubMerchantCodeValueObject.Create(v))
            .HasMaxLength(SubMerchantCodeValueObject.MaxLength)
            .IsRequired();

        // The business key that payments reference. Creates a unique index as well.
        builder.HasAlternateKey(s => s.Code);

        builder.Property(s => s.TradingName)
            .HasConversion(t => t.Value, v => TradingNameValueObject.Create(v))
            .HasMaxLength(TradingNameValueObject.MaxLength)
            .IsRequired();

        builder.Property(s => s.MerchantCategoryCode)
            .HasConversion(m => m.Value, v => MerchantCategoryCodeValueObject.Create(v))
            .HasMaxLength(4)
            .IsFixedLength()
            .IsRequired();

        builder.OwnsOne(s => s.Address, a =>
        {
            a.Property(x => x.City)
                .HasColumnName("AddressCity")
                .HasMaxLength(AddressValueObject.CityMaxLength)
                .IsRequired();

            a.Property(x => x.RegionCode)
                .HasColumnName("AddressRegionCode")
                .HasMaxLength(2)
                .IsFixedLength()
                .IsRequired();

            a.Property(x => x.CountryCode)
                .HasColumnName("AddressCountryCode")
                .HasMaxLength(2)
                .IsFixedLength()
                .IsRequired();
        });

        builder.Property(s => s.IsActive).IsRequired();
        builder.Property(s => s.CreatedOnUtc).IsRequired();
        builder.Property(s => s.CreatedBy).IsRequired();
        builder.Property(s => s.UpdatedOnUtc);
        builder.Property(s => s.UpdatedBy);
    }
}