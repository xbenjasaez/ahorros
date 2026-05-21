using Ahorro.Data;
using Ahorro.Models.Constants;
using Ahorro.Models.Entities;
using Ahorro.Models.Enums;
using Ahorro.Models.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ahorro.Configuration;

public static class DataSeeder
{
    public static readonly Guid DefaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static async Task SeedAsync(AppDbContext db, ICurrentUserContext userContext, CancellationToken ct = default)
    {
        await db.Database.EnsureCreatedAsync(ct);
        if (await db.UserProfiles.AnyAsync(ct))
        {
            await RestoreUserContextAsync(db, userContext, ct);
            await EnsureAppSettingsForUsersAsync(db, ct);
            return;
        }

        var user = new UserProfile
        {
            Id = DefaultUserId,
            DisplayName = "Benjamín",
            Email = "local@ahorro.app",
            IsLocal = true,
            CutoffDay = 25,
            DefaultFrequency = PeriodFrequency.Monthly
        };
        db.UserProfiles.Add(user);
        userContext.SetUser(user.Id);

        var categories = SeedCategories(user.Id);
        db.BudgetCategories.AddRange(categories);

        var card = new CreditCardAccount
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            UserProfileId = user.Id,
            Name = "Visa Principal",
            CreditLimit = 1_500_000,
            CurrentBalance = 420_000,
            AvailableCredit = 1_080_000,
            BillingDueDay = 5,
            MinimumPayment = 84_000
        };
        db.CreditCardAccounts.Add(card);

        var methods = new List<PaymentMethod>
        {
            new() { UserProfileId = user.Id, Name = "Efectivo", Type = PaymentMethodType.Cash },
            new() { UserProfileId = user.Id, Name = "Débito", Type = PaymentMethodType.Debit },
            new() { UserProfileId = user.Id, Name = "Visa", Type = PaymentMethodType.Credit, CreditCardAccountId = card.Id },
            new() { UserProfileId = user.Id, Name = "Transferencia", Type = PaymentMethodType.Transfer }
        };
        db.PaymentMethods.AddRange(methods);

        var periods = new[]
        {
            CreatePeriod(user.Id, new DateTime(2026, 2, 26), new DateTime(2026, 3, 25)),
            CreatePeriod(user.Id, new DateTime(2026, 3, 26), new DateTime(2026, 4, 25)),
            CreatePeriod(user.Id, new DateTime(2026, 4, 26), new DateTime(2026, 5, 25))
        };
        db.BudgetPeriods.AddRange(periods);

        foreach (var p in periods)
        {
            db.IncomeSources.AddRange(
                new IncomeSource { UserProfileId = user.Id, BudgetPeriodId = p.Id, Name = "Sueldo", Type = IncomeType.Salary, GrossAmount = 2_800_000, NetAmount = 2_200_000, Date = p.StartDate, Frequency = IncomeFrequency.Monthly },
                new IncomeSource { UserProfileId = user.Id, BudgetPeriodId = p.Id, Name = "Freelance", Type = IncomeType.Freelance, GrossAmount = 350_000, NetAmount = 280_000, Date = p.StartDate.AddDays(10), Frequency = IncomeFrequency.Monthly });

            p.TotalGrossIncome = 3_150_000;
            p.TotalNetIncome = 2_480_000;
            p.PlannedBudget = 2_480_000;
        }

        var activePeriod = periods[2];
        activePeriod.ActualSpent = 1_890_000;
        activePeriod.ExecutionPercent = 76.2m;
        activePeriod.Difference = activePeriod.PlannedBudget - activePeriod.ActualSpent;

        SeedAllocations(db, periods, categories);
        SeedGoals(db, user.Id, categories);
        SeedScheduledPayments(db, user.Id, categories, methods);
        SeedDebt(db, user.Id);
        db.AlertRules.Add(new AlertRule { UserProfileId = user.Id, AttentionThreshold = 80, LimitThreshold = 100 });
        SeedAppSettings(db, user.Id);

        await db.SaveChangesAsync(ct);
        SeedTransactions(db, periods, categories, methods, ct);
        await db.SaveChangesAsync(ct);

        userContext.ActivePeriodId = activePeriod.Id;
    }

    private static async Task EnsureAppSettingsForUsersAsync(AppDbContext db, CancellationToken ct)
    {
        var userIds = await db.UserProfiles.Select(u => u.Id).ToListAsync(ct);
        foreach (var userId in userIds)
        {
            if (await db.AppSettings.AnyAsync(s => s.UserProfileId == userId, ct))
                continue;
            SeedAppSettings(db, userId);
        }
        await db.SaveChangesAsync(ct);
    }

    private static void SeedAppSettings(AppDbContext db, Guid userId)
    {
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var defaults = new Dictionary<string, string>
        {
            [AppSettingKeys.CurrencyCode] = "CLP",
            [AppSettingKeys.ThemeVariant] = "dark-premium",
            [AppSettingKeys.AccentHex] = "#27D3FF",
            [AppSettingKeys.GoalDefaultMonthlyPace] = "50000",
            [AppSettingKeys.GoalShowProjections] = "True",
            [AppSettingKeys.GoalAutoCelebrate] = "True",
            [AppSettingKeys.GoalSuggestContributions] = "True",
            [AppSettingKeys.ExportDefaultFolder] = Path.Combine(docs, "Ahorro", "Exportaciones"),
            [AppSettingKeys.ExportIncludeNotes] = "True",
            [AppSettingKeys.ExportPdfCharts] = "True",
            [AppSettingKeys.ExportExcelAutoOpen] = "False",
            [AppSettingKeys.ExportFileNamePrefix] = "Ahorro",
            [AppSettingKeys.MultiUserEnabled] = "False"
        };

        foreach (var (key, value) in defaults)
        {
            db.AppSettings.Add(new AppSetting
            {
                UserProfileId = userId,
                Key = key,
                Value = value
            });
        }
    }

    private static async Task RestoreUserContextAsync(AppDbContext db, ICurrentUserContext userContext, CancellationToken ct)
    {
        var userId = await db.UserProfiles
            .Where(u => u.Id == DefaultUserId)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(ct);

        if (userId == Guid.Empty)
            userId = await db.UserProfiles.Select(u => u.Id).FirstAsync(ct);

        userContext.SetUser(userId);

        var today = DateTime.Today;
        var activePeriodId = await db.BudgetPeriods
            .Where(p => p.UserProfileId == userId && p.StartDate <= today && p.EndDate >= today)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct);

        if (activePeriodId.HasValue)
            userContext.ActivePeriodId = activePeriodId;
    }

    private static List<BudgetCategory> SeedCategories(Guid userId)
    {
        var data = new (string Name, string Color, BudgetGroup Group, int Order, string[]? Subs)[]
        {
            ("Ahorro", "#35E0A1", BudgetGroup.Savings, 1, null),
            ("Auto", "#27D3FF", BudgetGroup.Wants, 2, new[] { "Cambio de aceite", "Amortiguadores", "Alerón", "Mantención", "Repuestos", "Motor nuevo", "Otros" }),
            ("Comida", "#FFB84D", BudgetGroup.Needs, 3, null),
            ("Bencina", "#27D3FF", BudgetGroup.Needs, 4, null),
            ("Ocio", "#9B7AFF", BudgetGroup.Wants, 5, null),
            ("Deudas", "#FF6B6B", BudgetGroup.Needs, 6, null),
            ("Cuentas del hogar", "#35E0A1", BudgetGroup.Needs, 7, null),
            ("Transporte", "#27D3FF", BudgetGroup.Needs, 8, null),
            ("Salud", "#FFB84D", BudgetGroup.Needs, 9, null),
            ("Otros", "#93A4BD", BudgetGroup.Other, 10, null)
        };

        var list = new List<BudgetCategory>();
        foreach (var (name, color, group, order, subs) in data)
        {
            var cat = new BudgetCategory
            {
                UserProfileId = userId,
                Name = name,
                ColorHex = color,
                IconKey = name.ToLower(),
                DefaultGroup = group,
                SortOrder = order,
                AllowRollover = name == "Ahorro"
            };
            if (subs != null)
            {
                var i = 0;
                foreach (var s in subs)
                    cat.Subcategories.Add(new BudgetSubcategory { Name = s, SortOrder = ++i });
            }
            list.Add(cat);
        }
        return list;
    }

    private static BudgetPeriod CreatePeriod(Guid userId, DateTime start, DateTime end) => new()
    {
        UserProfileId = userId,
        StartDate = start,
        EndDate = end,
        Frequency = PeriodFrequency.Monthly
    };

    private static void SeedAllocations(AppDbContext db, BudgetPeriod[] periods, List<BudgetCategory> categories)
    {
        foreach (var period in periods)
        {
            var net = period.TotalNetIncome;
            foreach (var cat in categories)
            {
                var pct = cat.DefaultGroup switch
                {
                    BudgetGroup.Needs => 50m / 6,
                    BudgetGroup.Wants => 30m / 3,
                    BudgetGroup.Savings => 20m,
                    _ => 5m
                };
                var planned = net * (pct / 100m);
                var actual = planned * (cat.Name switch
                {
                    "Comida" => 0.92m,
                    "Ocio" => 1.12m,
                    "Bencina" => 0.85m,
                    "Auto" => 0.45m,
                    _ => 0.6m
                });
                if (period == periods[2] && cat.Name == "Ocio") actual = planned * 1.12m;

                var used = planned > 0 ? Math.Round(actual / planned * 100, 1) : 0;
                if (cat.Subcategories.Any())
                {
                    foreach (var sub in cat.Subcategories)
                    {
                        var subPlanned = planned / cat.Subcategories.Count;
                        var subActual = actual / cat.Subcategories.Count;
                        db.BudgetAllocations.Add(CreateAllocation(period.Id, cat.Id, sub.Id, subPlanned, subActual, pct));
                    }
                }
                else
                {
                    db.BudgetAllocations.Add(CreateAllocation(period.Id, cat.Id, null, planned, actual, pct));
                }
            }
        }
    }

    private static BudgetAllocation CreateAllocation(Guid periodId, Guid catId, Guid? subId, decimal planned, decimal actual, decimal pct)
    {
        var used = planned > 0 ? Math.Round(actual / planned * 100, 1) : 0;
        return new BudgetAllocation
        {
            BudgetPeriodId = periodId,
            CategoryId = catId,
            SubcategoryId = subId,
            AllocationMode = AllocationMode.Percentage,
            PlannedAmount = planned,
            PlannedPercent = pct,
            ActualAmount = actual,
            Difference = planned - actual,
            UsedPercent = used,
            Status = Helpers.BudgetStatusCalculator.FromUsedPercent(used)
        };
    }

    private static void SeedGoals(AppDbContext db, Guid userId, List<BudgetCategory> categories)
    {
        var ahorro = categories.First(c => c.Name == "Ahorro").Id;
        db.SavingsGoals.AddRange(
            new SavingsGoal { UserProfileId = userId, Name = "Casa", TargetAmount = 15_000_000, AccumulatedAmount = 4_200_000, TargetDate = new DateTime(2028, 6, 1), CategoryId = ahorro, ColorHex = "#27D3FF", IconKey = "home" },
            new SavingsGoal { UserProfileId = userId, Name = "Motor nuevo", TargetAmount = 3_500_000, AccumulatedAmount = 1_100_000, CategoryId = categories.First(c => c.Name == "Auto").Id, ColorHex = "#35E0A1", IconKey = "engine" },
            new SavingsGoal { UserProfileId = userId, Name = "Emergencia", TargetAmount = 2_000_000, AccumulatedAmount = 1_650_000, ColorHex = "#FFB84D", IconKey = "shield" },
            new SavingsGoal { UserProfileId = userId, Name = "Proyecto auto", TargetAmount = 800_000, AccumulatedAmount = 320_000, CategoryId = categories.First(c => c.Name == "Auto").Id, ColorHex = "#9B7AFF", IconKey = "car" });
    }

    private static void SeedScheduledPayments(AppDbContext db, Guid userId, List<BudgetCategory> categories, List<PaymentMethod> methods)
    {
        var cat = (string n) => categories.First(c => c.Name == n).Id;
        var visa = methods.First(m => m.Name == "Visa").Id;
        var debito = methods.First(m => m.Name == "Débito").Id;
        var efectivo = methods.First(m => m.Name == "Efectivo").Id;
        var transferencia = methods.First(m => m.Name == "Transferencia").Id;
        db.ScheduledPayments.AddRange(
            new ScheduledPayment { UserProfileId = userId, Name = "Plan celular", CategoryId = cat("Cuentas del hogar"), EstimatedAmount = 18_990, DueDate = DateTime.Today.AddDays(4), Frequency = IncomeFrequency.Monthly, ReminderDaysBefore = 3, PaymentMethodId = visa, Status = ScheduledPaymentStatus.Upcoming },
            new ScheduledPayment { UserProfileId = userId, Name = "Internet hogar", CategoryId = cat("Cuentas del hogar"), EstimatedAmount = 24_990, DueDate = DateTime.Today.AddDays(8), Frequency = IncomeFrequency.Monthly, ReminderDaysBefore = 5, PaymentMethodId = debito, Status = ScheduledPaymentStatus.Pending },
            new ScheduledPayment { UserProfileId = userId, Name = "Pago tarjeta Visa", CategoryId = cat("Deudas"), EstimatedAmount = 420_000, DueDate = DateTime.Today.AddDays(5), Frequency = IncomeFrequency.Monthly, ReminderDaysBefore = 7, PaymentMethodId = transferencia, Status = ScheduledPaymentStatus.Upcoming },
            new ScheduledPayment { UserProfileId = userId, Name = "Luz", CategoryId = cat("Cuentas del hogar"), EstimatedAmount = 45_000, DueDate = DateTime.Today.AddDays(-2), Frequency = IncomeFrequency.Monthly, ReminderDaysBefore = 3, PaymentMethodId = debito, LastPaidDate = DateTime.Today.AddMonths(-1), Status = ScheduledPaymentStatus.Overdue },
            new ScheduledPayment { UserProfileId = userId, Name = "Netflix", CategoryId = cat("Ocio"), EstimatedAmount = 12_990, DueDate = DateTime.Today.AddDays(12), Frequency = IncomeFrequency.Monthly, ReminderDaysBefore = 2, PaymentMethodId = debito, Status = ScheduledPaymentStatus.Pending },
            new ScheduledPayment { UserProfileId = userId, Name = "Agua", CategoryId = cat("Cuentas del hogar"), EstimatedAmount = 22_000, DueDate = DateTime.Today.AddDays(15), Frequency = IncomeFrequency.Monthly, ReminderDaysBefore = 4, PaymentMethodId = efectivo, Status = ScheduledPaymentStatus.Pending },
            new ScheduledPayment { UserProfileId = userId, Name = "Seguro auto", CategoryId = cat("Auto"), EstimatedAmount = 38_500, DueDate = DateTime.Today.AddDays(20), Frequency = IncomeFrequency.Monthly, ReminderDaysBefore = 10, PaymentMethodId = transferencia, Status = ScheduledPaymentStatus.Pending },
            new ScheduledPayment { UserProfileId = userId, Name = "Colegiatura", CategoryId = cat("Otros"), EstimatedAmount = 185_000, DueDate = DateTime.Today.AddDays(1), Frequency = IncomeFrequency.Monthly, ReminderDaysBefore = 5, PaymentMethodId = debito, Status = ScheduledPaymentStatus.Upcoming },
            new ScheduledPayment { UserProfileId = userId, Name = "Gimnasio", CategoryId = cat("Salud"), EstimatedAmount = 29_990, DueDate = DateTime.Today.AddDays(14), Frequency = IncomeFrequency.Biweekly, ReminderDaysBefore = 3, PaymentMethodId = visa, Status = ScheduledPaymentStatus.Pending },
            new ScheduledPayment { UserProfileId = userId, Name = "Patente vehículo", CategoryId = cat("Auto"), EstimatedAmount = 120_000, DueDate = DateTime.Today.AddDays(45), Frequency = IncomeFrequency.OneTime, ReminderDaysBefore = 14, PaymentMethodId = transferencia, Status = ScheduledPaymentStatus.Pending },
            new ScheduledPayment { UserProfileId = userId, Name = "Spotify", CategoryId = cat("Ocio"), EstimatedAmount = 5_990, DueDate = DateTime.Today.AddDays(-30), Frequency = IncomeFrequency.Monthly, ReminderDaysBefore = 2, PaymentMethodId = debito, LastPaidDate = DateTime.Today.AddDays(-30), Status = ScheduledPaymentStatus.Paid });
    }

    private static void SeedDebt(AppDbContext db, Guid userId) =>
        db.Debts.Add(new Debt
        {
            UserProfileId = userId,
            Name = "Crédito consumo",
            CurrentBalance = 890_000,
            RemainingBalance = 890_000,
            EstimatedInstallment = 78_000,
            DueDate = DateTime.Today.AddDays(18),
            InterestRate = 1.8m,
            Priority = 1,
            PaidThisMonth = 78_000
        });

    private static void SeedTransactions(AppDbContext db, BudgetPeriod[] periods, List<BudgetCategory> categories, List<PaymentMethod> methods, CancellationToken ct)
    {
        var active = periods[2];
        var goals = db.SavingsGoals.Select(g => g.Id).ToList();
        var rnd = new Random(42);
        var descs = new[] { "Supermercado Lider", "Restaurante sushi", "Uber al trabajo", "Farmacia Ahumada", "Copec combustible", "Spotify familiar", "Mantención auto", "Cine Hoyts", "Aporte meta Casa", "Sueldo mensual", "Freelance diseño", "Pago arriendo", "Gimnasio", "Regalo cumpleaños" };
        for (var i = 0; i < 45; i++)
        {
            var cat = categories[rnd.Next(categories.Count)];
            var sub = cat.Subcategories.Count > 0 ? cat.Subcategories.ElementAt(rnd.Next(cat.Subcategories.Count)) : null;
            var isIncome = i % 7 == 0;
            db.Transactions.Add(new MoneyTransaction
            {
                BudgetPeriodId = active.Id,
                Date = active.StartDate.AddDays(rnd.Next(0, 28)),
                Type = isIncome ? TransactionType.Income : TransactionType.Expense,
                Description = descs[rnd.Next(descs.Length)],
                CategoryId = cat.Id,
                SubcategoryId = sub?.Id,
                Amount = isIncome ? rnd.Next(800, 2500) * 1000 : rnd.Next(5, 120) * 1000,
                PaymentMethodId = methods[rnd.Next(methods.Count)].Id,
                Status = i % 5 == 0 ? TransactionStatus.Pending : i % 11 == 0 ? TransactionStatus.Cancelled : TransactionStatus.Paid,
                Note = i % 6 == 0 ? "Nota de seguimiento" : null,
                Tag = i % 3 == 0 ? "personal" : i % 5 == 0 ? "trabajo" : null,
                IsRecurring = i % 4 == 0,
                SavingsGoalId = goals.Count > 0 && i % 6 == 0 ? goals[rnd.Next(goals.Count)] : null
            });
        }
    }
}
