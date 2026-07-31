using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Budget.Web.Data;

/// <summary>Creates <see cref="BudgetDbContext"/> instances for design-time tooling such as EF migrations.</summary>
public class BudgetDbContextFactory : IDesignTimeDbContextFactory<BudgetDbContext>
{
    /// <summary>Creates a <see cref="BudgetDbContext"/> using the <c>BUDGET_DB_CONNECTION</c> environment variable or a local default.</summary>
    /// <param name="args">Design-time arguments passed by the EF tooling.</param>
    public BudgetDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("BUDGET_DB_CONNECTION")
            ?? "Host=localhost;Database=budget;Username=budget;Password=budget";

        var options = new DbContextOptionsBuilder<BudgetDbContext>()
            .UseNpgsql(connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new BudgetDbContext(options);
    }
}
