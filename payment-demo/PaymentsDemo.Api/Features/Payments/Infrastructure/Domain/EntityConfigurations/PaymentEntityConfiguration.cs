using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentsDemo.Api.Features.Payments.Infrastructure.Domain.Models;
using PaymentsDemo.Api.Infrastructure.Data.Domain.Models.SubMerchants;
using PaymentsDemo.Api.Infrastructure.Data.Domain.Models.ValueObjects;

namespace PaymentsDemo.Api.Features.Payments.Infrastructure.Domain.EntityConfigurations;

public class PaymentEntityConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", "payments", t =>
        {
            t.HasCheckConstraint("CK_Payments_Amount_Positive", "\"amount\" > 0");
            t.HasCheckConstraint("CK_Payments_Refunded_Range", "\"refunded_amount\" >= 0");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever(); // we assign the Guid ourselves

        builder.Property(p => p.CustomerId).IsRequired();
        builder.Property(p => p.CardId).IsRequired();

        builder.Property(p => p.SubMerchantCode)
            .HasConversion(c => c.Value, v => SubMerchantCodeValueObject.Create(v))
            .HasMaxLength(SubMerchantCodeValueObject.MaxLength)
            .IsRequired();

        builder.HasOne<SubMerchant>()
            .WithMany()
            .HasForeignKey(p => p.SubMerchantCode)
            .HasPrincipalKey(s => s.Code)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.Amount).IsRequired();

        builder.Property(p => p.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(36)   // canonical UUID with hyphens
            .IsFixedLength();

        builder.Property(p => p.TransactionId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.RetrievalReferenceNumber)
            .IsRequired()
            .HasMaxLength(12)
            .IsFixedLength();

        builder.Property(p => p.RequestHash)
            .IsRequired()
            .HasMaxLength(64)  // SHA-256 as hex
            .IsFixedLength();

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.CorrelationId)
            .IsRequired()
            .HasMaxLength(36);

        // Sensitive: contains the card token and the email. Never log this column.
        builder.Property(p => p.AcmeEnvelopeJson)
            .IsRequired();

        builder.Property(p => p.CreatedAtUtc).IsRequired();
        builder.Property(p => p.EnqueuedAtUtc);

        // Acme response data, filled in when the result arrives.
        builder.Property(p => p.AcmeResponseCode).HasMaxLength(3);
        builder.Property(p => p.ReconId).HasMaxLength(50);
        builder.Property(p => p.AuthCode).HasMaxLength(6);

        builder.Property(p => p.AuthAmount);
        builder.Property(p => p.RefundedAmount)
            .IsRequired()
            .HasDefaultValue(0L);

        // Uniqueness
        builder.HasIndex(p => p.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("UX_Payments_IdempotencyKey");

        builder.HasIndex(p => p.TransactionId)
            .IsUnique()
            .HasDatabaseName("UX_Payments_TransactionId");

        builder.HasIndex(p => p.RetrievalReferenceNumber)
            .IsUnique()
            .HasDatabaseName("UX_Payments_RetrievalReferenceNumber");

        // Adjustments are owned by the payment, so deleting a payment deletes them too.
        builder.Navigation(p => p.Adjustments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.Adjustments)
            .WithOne()
            .HasForeignKey(a => a.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for the in-flight check before a card is removed.
        builder.HasIndex(p => new { p.CardId, p.Status })
            .HasDatabaseName("IX_Payments_Card_Status");

        // Lookups used by the status endpoint and the results consumer
        builder.HasIndex(p => new { p.CustomerId, p.CreatedAtUtc })
            .HasDatabaseName("IX_Payments_Customer_CreatedAt");
    }
}