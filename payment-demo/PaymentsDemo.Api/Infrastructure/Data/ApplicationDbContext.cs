using Microsoft.EntityFrameworkCore;
using PaymentsDemo.Api.Infrastructure.Data.Domain;

namespace PaymentsDemo.Api.Infrastructure.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Merchant> Merchants { get; set; }
    public DbSet<CardDetail> CardDetails { get; set; }
    public DbSet<Customer> Customers { get; set; }
}