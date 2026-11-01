using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using PaymentsDemo.Api.Features.Cards.Infrastructure.Domain.Models;
using PaymentsDemo.Api.Features.Payments.Infrastructure.Domain.Models;
using PaymentsDemo.Api.Infrastructure.Data.Domain.Models.Customers;
using PaymentsDemo.Api.Infrastructure.Data.Domain.Models.Merchants;
using PaymentsDemo.Api.Infrastructure.Data.Domain.Models.SubMerchants;
using PaymentsDemo.Api.Infrastructure.Data.Domain.Models.ValueObjects;

namespace PaymentsDemo.Api.Infrastructure.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Merchant> Merchants { get; set; }
    public DbSet<SubMerchant> SubMerchants { get; set; }
    public DbSet<BankCard> BankCards { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Payment> Payments { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<SubMerchantCodeValueObject>()
            .HaveConversion<SubMerchantCodeConverter>()
            .HaveMaxLength(SubMerchantCodeValueObject.MaxLength);

        configurationBuilder.Properties<TradingNameValueObject>()
            .HaveConversion<TradingAsConverter>()
            .HaveMaxLength(TradingNameValueObject.MaxLength);

        configurationBuilder.Properties<MerchantCategoryCodeValueObject>()
            .HaveConversion<MerchantCategoryCodeConverter>()
            .HaveMaxLength(4)
            .AreFixedLength();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration<T> in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}