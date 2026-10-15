using Microsoft.EntityFrameworkCore;
using PaisaFlow.Core.Entities;

namespace PaisaFlow.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<RecurringTransaction> RecurringTransactions => Set<RecurringTransaction>();
    public DbSet<SplitGroup> SplitGroups => Set<SplitGroup>();
    public DbSet<SplitMember> SplitMembers => Set<SplitMember>();
    public DbSet<SplitExpense> SplitExpenses => Set<SplitExpense>();
    public DbSet<SplitParticipant> SplitParticipants => Set<SplitParticipant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Transaction Decimal precision
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.HasIndex(e => new { e.UserId, e.Date });
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.AccountId);
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.Property(e => e.Balance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(e => e.DefaultBudget).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Budget>(entity =>
        {
            entity.Property(e => e.LimitAmount).HasPrecision(18, 2);
            entity.HasIndex(e => new { e.UserId, e.Month, e.Year });
        });

        modelBuilder.Entity<RecurringTransaction>(entity =>
        {
            entity.Property(e => e.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.Property(e => e.MonthlyBudgetLimit).HasPrecision(18, 2);
        });

        modelBuilder.Entity<SplitExpense>(entity =>
        {
            entity.Property(e => e.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<SplitParticipant>(entity =>
        {
            entity.Property(e => e.ShareAmount).HasPrecision(18, 2);
        });
    }
}
