using System.Text.Json;
using Ahorro.Data;
using Ahorro.Models.Dtos;
using Ahorro.Models.Entities;
using Ahorro.Models.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ahorro.Services.Filters;

public class FilterPresetService : IFilterPresetService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserContext _user;

    public FilterPresetService(AppDbContext db, ICurrentUserContext user)
    {
        _db = db;
        _user = user;
    }

    public async Task SavePresetAsync(string name, FilterCriteria criteria, CancellationToken ct = default)
    {
        _db.FilterPresets.Add(new FilterPreset
        {
            UserProfileId = _user.UserId,
            Name = name,
            FilterJson = JsonSerializer.Serialize(criteria)
        });
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<FilterPreset>> GetPresetsAsync(CancellationToken ct = default) =>
        _db.FilterPresets.AsNoTracking()
            .Where(p => p.UserProfileId == _user.UserId)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
}
