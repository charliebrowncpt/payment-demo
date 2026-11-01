using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using PaymentsDemo.Api.Features.Cards.Infrastructure.Domain.Models;
using PaymentsDemo.Api.Features.Payments.Infrastructure.Domain.Models;
using PaymentsDemo.Api.Infrastructure.Data.Domain.Models.Customers;
using PaymentsDemo.Api.Infrastructure.Data.Domain.Models.Merchants;
using PaymentsDemo.Api.Infrastructure.Data.Domain.Models.SubMerchants;

namespace PaymentsDemo.Api.Infrastructure.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Merchant> Merchants { get; set; }
    public DbSet<SubMerchant> SubMerchants { get; set; }
    public DbSet<BankCard> BankCards { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Payment> Payments { get; set; }
}