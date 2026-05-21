using Ahorro.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ahorro.Data.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.DisplayName).HasMaxLength(120);
        b.Property(x => x.Email).HasMaxLength(200);
    }
}

public class BudgetPeriodConfiguration : IEntityTypeConfiguration<BudgetPeriod>
{
    public void Configure(EntityTypeBuilder<BudgetPeriod> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UserProfileId, x.StartDate });
        b.Property(x => x.TotalGrossIncome).HasPrecision(18, 2);
        b.Property(x => x.TotalNetIncome).HasPrecision(18, 2);
        b.Property(x => x.PlannedBudget).HasPrecision(18, 2);
        b.Property(x => x.ActualSpent).HasPrecision(18, 2);
        b.Property(x => x.Difference).HasPrecision(18, 2);
        b.Property(x => x.ExecutionPercent).HasPrecision(8, 2);
        b.HasOne(x => x.UserProfile).WithMany(u => u.BudgetPeriods).HasForeignKey(x => x.UserProfileId);
    }
}

public class MoneyTransactionConfiguration : IEntityTypeConfiguration<MoneyTransaction>
{
    public void Configure(EntityTypeBuilder<MoneyTransaction> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.Date);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Description).HasMaxLength(300);
        b.HasOne(x => x.BudgetPeriod).WithMany(p => p.Transactions).HasForeignKey(x => x.BudgetPeriodId);
        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BudgetAllocationConfiguration : IEntityTypeConfiguration<BudgetAllocation>
{
    public void Configure(EntityTypeBuilder<BudgetAllocation> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.PlannedAmount).HasPrecision(18, 2);
        b.Property(x => x.ActualAmount).HasPrecision(18, 2);
        b.Property(x => x.UsedPercent).HasPrecision(8, 2);
        b.HasOne(x => x.BudgetPeriod).WithMany(p => p.Allocations).HasForeignKey(x => x.BudgetPeriodId);
    }
}

public class BudgetCategoryConfiguration : IEntityTypeConfiguration<BudgetCategory>
{
    public void Configure(EntityTypeBuilder<BudgetCategory> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(100);
        b.HasMany(x => x.Subcategories).WithOne(s => s.Category).HasForeignKey(s => s.CategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SavingsGoalConfiguration : IEntityTypeConfiguration<SavingsGoal>
{
    public void Configure(EntityTypeBuilder<SavingsGoal> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.TargetAmount).HasPrecision(18, 2);
        b.Property(x => x.AccumulatedAmount).HasPrecision(18, 2);
    }
}

public class ScheduledPaymentConfiguration : IEntityTypeConfiguration<ScheduledPayment>
{
    public void Configure(EntityTypeBuilder<ScheduledPayment> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.EstimatedAmount).HasPrecision(18, 2);
        b.HasIndex(x => x.DueDate);
    }
}

public class DebtConfiguration : IEntityTypeConfiguration<Debt>
{
    public void Configure(EntityTypeBuilder<Debt> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.CurrentBalance).HasPrecision(18, 2);
        b.Property(x => x.RemainingBalance).HasPrecision(18, 2);
    }
}

public class CreditCardAccountConfiguration : IEntityTypeConfiguration<CreditCardAccount>
{
    public void Configure(EntityTypeBuilder<CreditCardAccount> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.CreditLimit).HasPrecision(18, 2);
        b.Property(x => x.CurrentBalance).HasPrecision(18, 2);
        b.Property(x => x.AvailableCredit).HasPrecision(18, 2);
    }
}
