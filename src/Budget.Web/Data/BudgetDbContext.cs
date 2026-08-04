using Budget.Web.Domain.Merchants;
using Budget.Web.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Budget.Web.Data;

public class BudgetDbContext(DbContextOptions<BudgetDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<MerchantAlias> MerchantAliases => Set<MerchantAlias>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(t => t.Amount).HasPrecision(10, 2);
            entity.HasIndex(t => new { t.Iban, t.FollowNumber, t.UserId }).IsUnique();
            entity.HasIndex(t => t.NameOtherPartyNormalized);
        });

        modelBuilder.Entity<Merchant>(entity =>
        {
            entity.HasIndex(m => m.NameNormalized).IsUnique();
        });

        modelBuilder.Entity<MerchantAlias>(entity =>
        {
            entity.HasIndex(a => a.NameNormalized).IsUnique();
            entity.HasOne(a => a.Merchant)
                .WithMany(m => m.Aliases)
                .HasForeignKey(a => a.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
