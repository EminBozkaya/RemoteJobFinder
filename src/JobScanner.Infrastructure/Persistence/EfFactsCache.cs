using JobScanner.Application.Abstractions;
using JobScanner.Domain.Eligibility;
using Microsoft.EntityFrameworkCore;

namespace JobScanner.Infrastructure.Persistence;

/// <summary>
/// HAM gerceklerin EF cache'i. Anahtar: JobId + PromptVersion + ModelVersion + VersionHash.
/// Karar/verdict ASLA cache'lenmez.
/// </summary>
public sealed class EfFactsCache : IFactsCache
{
    private readonly JobScannerDbContext _db;

    public EfFactsCache(JobScannerDbContext db) => _db = db;

    public async Task<EligibilityFacts?> GetAsync(
        long jobId, string promptVersion, string modelVersion, string versionHash, CancellationToken ct) =>
        await _db.EligibilityFactsCache.AsNoTracking().FirstOrDefaultAsync(
            f => f.JobId == jobId &&
                 f.PromptVersion == promptVersion &&
                 f.ModelVersion == modelVersion &&
                 f.VersionHash == versionHash, ct);

    public async Task SetAsync(EligibilityFacts facts, CancellationToken ct)
    {
        var exists = await _db.EligibilityFactsCache.AnyAsync(
            f => f.JobId == facts.JobId &&
                 f.PromptVersion == facts.PromptVersion &&
                 f.ModelVersion == facts.ModelVersion &&
                 f.VersionHash == facts.VersionHash, ct);

        if (exists) return; // cache idempotent: ayni anahtar tekrar yazilmaz

        _db.EligibilityFactsCache.Add(facts);
        await _db.SaveChangesAsync(ct);
    }
}
