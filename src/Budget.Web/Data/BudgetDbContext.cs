using Budget.Web.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Budget.Web.Data;

public class BudgetDbContext(DbContextOptions<BudgetDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(t => t.Amount).HasPrecision(10, 2);
            entity.HasIndex(t => new { t.Iban, t.FollowNumber, t.UserId }).IsUnique();
        });
    }
}
