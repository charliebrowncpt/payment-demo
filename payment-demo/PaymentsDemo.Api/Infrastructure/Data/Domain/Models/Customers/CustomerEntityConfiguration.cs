namespace PaymentsDemo.Api.Infrastructure.Data.Domain.Models.Customers;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CustomerEntityConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", "payments");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasMaxLength(50).ValueGeneratedNever();
        builder.Property(c => c.CreatedOnUtc).IsRequired();

        // EF writes through the private _cards field, not the read-only property.
        builder.Navigation(c => c.Cards)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(c => c.Cards)
            .WithOne()
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}