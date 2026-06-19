using JobScanner.Application.Abstractions;
using JobScanner.Domain.Applications;
using Microsoft.EntityFrameworkCore;

namespace JobScanner.Infrastructure.Persistence;

/// <summary>EF Core (Npgsql) başvuru materyali deposu. Anahtar = (ProfileId, JobId).</summary>
public sealed class EfApplicationMaterialRepository : IApplicationMaterialRepository
{
    private readonly JobScannerDbContext _db;

    public EfApplicationMaterialRepository(JobScannerDbContext db) => _db = db;

    public async Task<ApplicationMaterial?> GetAsync(long profileId, long jobId, CancellationToken ct) =>
        await _db.ApplicationMaterials
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ProfileId == profileId && m.JobId == jobId, ct);

    public async Task SaveAsync(ApplicationMaterial material, CancellationToken ct)
    {
        var existing = await _db.ApplicationMaterials
            .FirstOrDefaultAsync(m => m.ProfileId == material.ProfileId && m.JobId == material.JobId, ct);

        if (existing is null)
        {
            _db.ApplicationMaterials.Add(material);
        }
        else
        {
            existing.CoverLetter = material.CoverLetter;
            existing.TailoredCvMarkdown = material.TailoredCvMarkdown;
            existing.Language = material.Language;
            existing.SourceCvHash = material.SourceCvHash;
            existing.PromptVersion = material.PromptVersion;
            existing.ModelVersion = material.ModelVersion;
            existing.JobVersionHash = material.JobVersionHash;
            existing.GeneratedAt = material.GeneratedAt;
        }

        await _db.SaveChangesAsync(ct);
    }
}
