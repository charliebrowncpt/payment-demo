using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentsDemo.Api.Features.Payments.Infrastructure.Domain.Models;

namespace PaymentsDemo.Api.Features.Payments.Infrastructure.Domain.EntityConfigurations;

public class PaymentAdjustmentEntityConfiguration : IEntityTypeConfiguration<PaymentAdjustment>
{
    public void Configure(EntityTypeBuilder<PaymentAdjustment> builder)
    {
        builder.ToTable("PaymentAdjustments", "payments", t =>
        {
            t.HasCheckConstraint("CK_PaymentAdjustments_Amount_Positive", "\"Amount\" > 0");
        });

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.PaymentId).IsRequired();
        builder.Property(a => a.EventId).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Type).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Amount).IsRequired();
        builder.Property(a => a.ReceivedAtUtc).IsRequired();

        // The dedupe guarantee for Switch events.
        builder.HasIndex(a => new { a.PaymentId, a.EventId })
            .IsUnique()
            .HasDatabaseName("UX_PaymentAdjustments_Payment_Event");
    }
}