using JobScanner.Application.Abstractions;
using JobScanner.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace JobScanner.Infrastructure.Persistence;

public sealed class EfProfileRepository : IProfileRepository
{
    private readonly JobScannerDbContext _db;

    public EfProfileRepository(JobScannerDbContext db) => _db = db;

    public async Task<IReadOnlyList<CriteriaProfile>> GetActiveAsync(CancellationToken ct) =>
        await _db.CriteriaProfiles
            .AsNoTracking()
            .Where(p => p.IsActive)
            .ToListAsync(ct);

    public async Task<bool> UpdateAsync(long profileId, ProfileEdit edit, CancellationToken ct)
    {
        var profile = await _db.CriteriaProfiles.FirstOrDefaultAsync(p => p.Id == profileId, ct);
        if (profile is null) return false;

        profile.ResidenceCountry = edit.ResidenceCountry;
        profile.ForbiddenKeywords = edit.ForbiddenKeywords;
        profile.Skills = edit.Skills;
        profile.Languages = edit.Languages;
        profile.SoftSkills = edit.SoftSkills;
        profile.TimezoneToleranceHours = edit.TimezoneToleranceHours;
        profile.MinScoreToShow = edit.MinScoreToShow;

        await _db.SaveChangesAsync(ct);
        return true;
    }
}
