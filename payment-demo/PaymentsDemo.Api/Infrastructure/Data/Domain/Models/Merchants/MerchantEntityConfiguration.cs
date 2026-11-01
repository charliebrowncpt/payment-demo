namespace PaymentsDemo.Api.Infrastructure.Data.Domain.Models.Merchants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class MerchantEntityConfiguration : IEntityTypeConfiguration<Merchant>
{
    public void Configure(EntityTypeBuilder<Merchant> builder)
    {
        builder.ToTable("Merchants", "payments");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.LegalName).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Status).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.Property(m => m.CreatedOnUtc).IsRequired();
        builder.Property(m => m.CreatedBy).IsRequired();
        builder.Property(m => m.UpdatedOnUtc);
        builder.Property(m => m.UpdatedBy);

        builder.Navigation(m => m.SubMerchants)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(m => m.SubMerchants)
               .WithOne()
               .HasForeignKey(s => s.MerchantId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
