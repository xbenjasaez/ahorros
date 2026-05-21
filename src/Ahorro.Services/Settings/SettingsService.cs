using System.IO;
using Ahorro.Data;
using Ahorro.Models.Abstractions;
using Ahorro.Models.Constants;
using Ahorro.Models.Dtos;
using Ahorro.Models.Entities;
using Ahorro.Models.Enums;
using Ahorro.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ahorro.Services.Settings;

public class SettingsService : ISettingsService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserContext _user;
    private readonly IBudgetPeriodService _periods;

    public SettingsService(AppDbContext db, ICurrentUserContext user, IBudgetPeriodService periods)
    {
        _db = db;
        _user = user;
        _periods = periods;
    }

    public Task<UserProfile> GetProfileAsync(CancellationToken ct = default) =>
        _db.UserProfiles.AsNoTracking().FirstAsync(u => u.Id == _user.UserId, ct);

    public async Task SaveProfileAsync(UserProfile profile, CancellationToken ct = default)
    {
        var entity = await _db.UserProfiles.FirstAsync(u => u.Id == _user.UserId, ct);
        entity.DisplayName = profile.DisplayName.Trim();
        entity.Email = profile.Email?.Trim();
        entity.CutoffDay = Math.Clamp(profile.CutoffDay, 1, 28);
        entity.DefaultFrequency = profile.DefaultFrequency;
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<UserProfile>> GetProfilesAsync(CancellationToken ct = default) =>
        _db.UserProfiles.AsNoTracking().OrderBy(u => u.DisplayName).ToListAsync(ct);

    public async Task SwitchProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var exists = await _db.UserProfiles.AnyAsync(u => u.Id == userId, ct);
        if (!exists)
            throw new InvalidOperationException("Perfil no encontrado.");

        _user.SetUser(userId);
        var today = DateTime.Today;
        var periodId = await _db.BudgetPeriods
            .Where(p => p.UserProfileId == userId && p.StartDate <= today && p.EndDate >= today)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct);

        _user.ActivePeriodId = periodId;
    }

    public async Task<UserPreferences> GetPreferencesAsync(CancellationToken ct = default)
    {
        var map = await _db.AppSettings.AsNoTracking()
            .Where(s => s.UserProfileId == _user.UserId)
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);

        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return new UserPreferences
        {
            CurrencyCode = Get(map, AppSettingKeys.CurrencyCode, "CLP"),
            ThemeVariant = Get(map, AppSettingKeys.ThemeVariant, "dark-premium"),
            AccentHex = Get(map, AppSettingKeys.AccentHex, "#27D3FF"),
            GoalDefaultMonthlyPace = decimal.TryParse(Get(map, AppSettingKeys.GoalDefaultMonthlyPace, "50000"), out var pace) ? pace : 50_000m,
            GoalShowProjections = GetBool(map, AppSettingKeys.GoalShowProjections, true),
            GoalAutoCelebrate = GetBool(map, AppSettingKeys.GoalAutoCelebrate, true),
            GoalSuggestContributions = GetBool(map, AppSettingKeys.GoalSuggestContributions, true),
            ExportDefaultFolder = Get(map, AppSettingKeys.ExportDefaultFolder, Path.Combine(docs, "Ahorro", "Exportaciones")),
            ExportIncludeNotes = GetBool(map, AppSettingKeys.ExportIncludeNotes, true),
            ExportPdfCharts = GetBool(map, AppSettingKeys.ExportPdfCharts, true),
            ExportExcelAutoOpen = GetBool(map, AppSettingKeys.ExportExcelAutoOpen, false),
            ExportFileNamePrefix = Get(map, AppSettingKeys.ExportFileNamePrefix, "Ahorro"),
            MultiUserEnabled = GetBool(map, AppSettingKeys.MultiUserEnabled, false)
        };
    }

    public async Task SavePreferencesAsync(UserPreferences preferences, CancellationToken ct = default)
    {
        await UpsertAsync(AppSettingKeys.CurrencyCode, preferences.CurrencyCode, ct);
        await UpsertAsync(AppSettingKeys.ThemeVariant, preferences.ThemeVariant, ct);
        await UpsertAsync(AppSettingKeys.AccentHex, preferences.AccentHex, ct);
        await UpsertAsync(AppSettingKeys.GoalDefaultMonthlyPace, preferences.GoalDefaultMonthlyPace.ToString("0"), ct);
        await UpsertAsync(AppSettingKeys.GoalShowProjections, preferences.GoalShowProjections.ToString(), ct);
        await UpsertAsync(AppSettingKeys.GoalAutoCelebrate, preferences.GoalAutoCelebrate.ToString(), ct);
        await UpsertAsync(AppSettingKeys.GoalSuggestContributions, preferences.GoalSuggestContributions.ToString(), ct);
        await UpsertAsync(AppSettingKeys.ExportDefaultFolder, preferences.ExportDefaultFolder, ct);
        await UpsertAsync(AppSettingKeys.ExportIncludeNotes, preferences.ExportIncludeNotes.ToString(), ct);
        await UpsertAsync(AppSettingKeys.ExportPdfCharts, preferences.ExportPdfCharts.ToString(), ct);
        await UpsertAsync(AppSettingKeys.ExportExcelAutoOpen, preferences.ExportExcelAutoOpen.ToString(), ct);
        await UpsertAsync(AppSettingKeys.ExportFileNamePrefix, preferences.ExportFileNamePrefix, ct);
        await UpsertAsync(AppSettingKeys.MultiUserEnabled, preferences.MultiUserEnabled.ToString(), ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AlertRule> GetGlobalAlertRuleAsync(CancellationToken ct = default)
    {
        var rule = await _db.AlertRules.AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserProfileId == _user.UserId && r.CategoryId == null, ct);

        if (rule != null) return rule;

        return new AlertRule
        {
            UserProfileId = _user.UserId,
            AttentionThreshold = 80,
            LimitThreshold = 100,
            IsEnabled = true
        };
    }

    public async Task SaveGlobalAlertRuleAsync(int attentionThreshold, int limitThreshold, bool isEnabled, CancellationToken ct = default)
    {
        var rule = await _db.AlertRules
            .FirstOrDefaultAsync(r => r.UserProfileId == _user.UserId && r.CategoryId == null, ct);

        if (rule == null)
        {
            rule = new AlertRule { UserProfileId = _user.UserId };
            _db.AlertRules.Add(rule);
        }

        rule.AttentionThreshold = Math.Clamp(attentionThreshold, 50, 99);
        rule.LimitThreshold = Math.Clamp(limitThreshold, rule.AttentionThreshold + 1, 150);
        rule.IsEnabled = isEnabled;
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<BudgetCategory>> GetCategoriesAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var q = _db.BudgetCategories.AsNoTracking()
            .Include(c => c.Subcategories)
            .Where(c => c.UserProfileId == _user.UserId);

        if (!includeInactive)
            q = q.Where(c => c.IsActive);

        return q.OrderBy(c => c.SortOrder).ToListAsync(ct);
    }

    public async Task<BudgetCategory> AddCategoryAsync(CategoryUpsert data, CancellationToken ct = default)
    {
        var name = data.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre de la categoría es obligatorio.");

        var maxOrder = await _db.BudgetCategories
            .Where(c => c.UserProfileId == _user.UserId)
            .Select(c => (int?)c.SortOrder)
            .MaxAsync(ct) ?? 0;

        var category = new BudgetCategory
        {
            UserProfileId = _user.UserId,
            Name = name,
            DefaultGroup = data.DefaultGroup,
            ColorHex = data.ColorHex,
            IconKey = data.IconKey,
            AllowRollover = data.AllowRollover,
            AllocationMode = data.AllocationMode,
            SortOrder = maxOrder + 1
        };

        _db.BudgetCategories.Add(category);

        var period = await _periods.EnsureActivePeriodAsync(ct);
        _db.BudgetAllocations.Add(new BudgetAllocation
        {
            BudgetPeriodId = period.Id,
            CategoryId = category.Id,
            AllocationMode = AllocationMode.Manual,
            PlannedAmount = 0,
            ActualAmount = 0,
            Difference = 0,
            UsedPercent = 0,
            Status = BudgetLineStatus.Normal
        });

        await _db.SaveChangesAsync(ct);
        return category;
    }

    public async Task UpdateCategoryAsync(Guid id, CategoryUpsert data, CancellationToken ct = default)
    {
        var category = await _db.BudgetCategories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserProfileId == _user.UserId, ct)
            ?? throw new InvalidOperationException("Categoría no encontrada.");

        category.Name = data.Name.Trim();
        category.DefaultGroup = data.DefaultGroup;
        category.ColorHex = data.ColorHex;
        category.IconKey = data.IconKey;
        category.AllowRollover = data.AllowRollover;
        category.AllocationMode = data.AllocationMode;
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetCategoryActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var category = await _db.BudgetCategories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserProfileId == _user.UserId, ct)
            ?? throw new InvalidOperationException("Categoría no encontrada.");

        category.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ReorderCategoryAsync(Guid id, bool moveUp, CancellationToken ct = default)
    {
        var list = await _db.BudgetCategories
            .Where(c => c.UserProfileId == _user.UserId)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

        var index = list.FindIndex(c => c.Id == id);
        if (index < 0) return;

        var swapIndex = moveUp ? index - 1 : index + 1;
        if (swapIndex < 0 || swapIndex >= list.Count) return;

        (list[index].SortOrder, list[swapIndex].SortOrder) = (list[swapIndex].SortOrder, list[index].SortOrder);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<BudgetSubcategory> AddSubcategoryAsync(Guid categoryId, string name, CancellationToken ct = default)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new ArgumentException("El nombre de la subcategoría es obligatorio.");

        var category = await _db.BudgetCategories
            .Include(c => c.Subcategories)
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserProfileId == _user.UserId, ct)
            ?? throw new InvalidOperationException("Categoría no encontrada.");

        var maxOrder = category.Subcategories.Select(s => s.SortOrder).DefaultIfEmpty(0).Max();
        var sub = new BudgetSubcategory
        {
            CategoryId = categoryId,
            Name = trimmed,
            SortOrder = maxOrder + 1
        };

        _db.BudgetSubcategories.Add(sub);

        var period = await _periods.EnsureActivePeriodAsync(ct);
        _db.BudgetAllocations.Add(new BudgetAllocation
        {
            BudgetPeriodId = period.Id,
            CategoryId = categoryId,
            SubcategoryId = sub.Id,
            AllocationMode = AllocationMode.Manual,
            PlannedAmount = 0,
            ActualAmount = 0,
            Difference = 0,
            UsedPercent = 0,
            Status = BudgetLineStatus.Normal
        });

        await _db.SaveChangesAsync(ct);
        return sub;
    }

    public async Task UpdateSubcategoryAsync(Guid id, string name, CancellationToken ct = default)
    {
        var sub = await _db.BudgetSubcategories
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == id && s.Category!.UserProfileId == _user.UserId, ct)
            ?? throw new InvalidOperationException("Subcategoría no encontrada.");

        sub.Name = name.Trim();
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetSubcategoryActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var sub = await _db.BudgetSubcategories
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == id && s.Category!.UserProfileId == _user.UserId, ct)
            ?? throw new InvalidOperationException("Subcategoría no encontrada.");

        sub.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<PaymentMethod>> GetPaymentMethodsAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var q = _db.PaymentMethods.AsNoTracking()
            .Include(m => m.CreditCardAccount)
            .Where(m => m.UserProfileId == _user.UserId);

        if (!includeInactive)
            q = q.Where(m => m.IsActive);

        return q.OrderBy(m => m.Name).ToListAsync(ct);
    }

    public Task<List<CreditCardAccount>> GetCreditCardsAsync(CancellationToken ct = default) =>
        _db.CreditCardAccounts.AsNoTracking()
            .Where(c => c.UserProfileId == _user.UserId && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<PaymentMethod> SavePaymentMethodAsync(PaymentMethodUpsert data, CancellationToken ct = default)
    {
        var name = data.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del método de pago es obligatorio.");

        PaymentMethod method;
        if (data.Id.HasValue && data.Id != Guid.Empty)
        {
            method = await _db.PaymentMethods
                .FirstOrDefaultAsync(m => m.Id == data.Id && m.UserProfileId == _user.UserId, ct)
                ?? throw new InvalidOperationException("Método de pago no encontrado.");
        }
        else
        {
            method = new PaymentMethod { UserProfileId = _user.UserId };
            _db.PaymentMethods.Add(method);
        }

        method.Name = name;
        method.Type = data.Type;
        method.CreditCardAccountId = data.Type == PaymentMethodType.Credit ? data.CreditCardAccountId : null;
        method.IsActive = data.IsActive;
        await _db.SaveChangesAsync(ct);
        return method;
    }

    public async Task SetPaymentMethodActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var method = await _db.PaymentMethods
            .FirstOrDefaultAsync(m => m.Id == id && m.UserProfileId == _user.UserId, ct)
            ?? throw new InvalidOperationException("Método de pago no encontrado.");

        method.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<ExportHistory>> GetExportHistoryAsync(int take = 20, CancellationToken ct = default) =>
        _db.ExportHistories.AsNoTracking()
            .Where(e => e.UserProfileId == _user.UserId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

    private async Task UpsertAsync(string key, string value, CancellationToken ct)
    {
        var row = await _db.AppSettings
            .FirstOrDefaultAsync(s => s.UserProfileId == _user.UserId && s.Key == key, ct);

        if (row == null)
        {
            _db.AppSettings.Add(new AppSetting
            {
                UserProfileId = _user.UserId,
                Key = key,
                Value = value
            });
        }
        else
        {
            row.Value = value;
        }
    }

    private static string Get(Dictionary<string, string> map, string key, string fallback) =>
        map.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;

    private static bool GetBool(Dictionary<string, string> map, string key, bool fallback) =>
        map.TryGetValue(key, out var v) && bool.TryParse(v, out var b) ? b : fallback;
}
