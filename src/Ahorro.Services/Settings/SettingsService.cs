using Ahorro.Data;
using Ahorro.Models.Entities;
using Ahorro.Models.Abstractions;
using Ahorro.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ahorro.Services.Settings;

public class SettingsService : ISettingsService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserContext _user;

    public SettingsService(AppDbContext db, ICurrentUserContext user)
    {
        _db = db;
        _user = user;
    }

    public Task<UserProfile> GetProfileAsync(CancellationToken ct = default) =>
        _db.UserProfiles.AsNoTracking().FirstAsync(u => u.Id == _user.UserId, ct);

    public async Task SaveProfileAsync(UserProfile profile, CancellationToken ct = default)
    {
        _db.UserProfiles.Update(profile);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<BudgetCategory>> GetCategoriesAsync(CancellationToken ct = default) =>
        _db.BudgetCategories.AsNoTracking()
            .Include(c => c.Subcategories)
            .Where(c => c.UserProfileId == _user.UserId)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);
}
