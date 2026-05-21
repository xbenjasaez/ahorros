using Ahorro.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ahorro.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<BudgetPeriod> BudgetPeriods => Set<BudgetPeriod>();
    public DbSet<IncomeSource> IncomeSources => Set<IncomeSource>();
    public DbSet<BudgetCategory> BudgetCategories => Set<BudgetCategory>();
    public DbSet<BudgetSubcategory> BudgetSubcategories => Set<BudgetSubcategory>();
    public DbSet<BudgetAllocation> BudgetAllocations => Set<BudgetAllocation>();
    public DbSet<MoneyTransaction> Transactions => Set<MoneyTransaction>();
    public DbSet<SavingsGoal> SavingsGoals => Set<SavingsGoal>();
    public DbSet<GoalContribution> GoalContributions => Set<GoalContribution>();
    public DbSet<ScheduledPayment> ScheduledPayments => Set<ScheduledPayment>();
    public DbSet<Debt> Debts => Set<Debt>();
    public DbSet<CreditCardAccount> CreditCardAccounts => Set<CreditCardAccount>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<ExportHistory> ExportHistories => Set<ExportHistory>();
    public DbSet<FilterPreset> FilterPresets => Set<FilterPreset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
