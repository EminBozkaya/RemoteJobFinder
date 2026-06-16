using JobScanner.Application.Abstractions;
using JobScanner.Domain.Jobs;
using Microsoft.EntityFrameworkCore;

namespace JobScanner.Infrastructure.Persistence;

/// <summary>EF Core (Npgsql) ilan deposu. Kimlik = (SourceName, ExternalId).</summary>
public sealed class EfJobRepository : IJobRepository
{
    private readonly JobScannerDbContext _db;

    public EfJobRepository(JobScannerDbContext db) => _db = db;

    public async Task<JobPosting?> FindByIdentityAsync(string sourceName, string externalId, CancellationToken ct) =>
        await _db.JobPostings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SourceName == sourceName && x.ExternalId == externalId, ct);

    public async Task<JobPosting> UpsertAsync(JobPosting job, CancellationToken ct)
    {
        var existing = await _db.JobPostings
            .FirstOrDefaultAsync(x => x.SourceName == job.SourceName && x.ExternalId == job.ExternalId, ct);

        if (existing is null)
        {
            _db.JobPostings.Add(job);
            await _db.SaveChangesAsync(ct);
            return job;
        }

        // Icerik degisti: kimlik + FirstSeenAt korunur, geri kalan guncellenir.
        existing.Title = job.Title;
        existing.Company = job.Company;
        existing.DescriptionText = job.DescriptionText;
        existing.Url = job.Url;
        existing.ApplyUrl = job.ApplyUrl;
        existing.WorkMode = job.WorkMode;
        existing.PostedAt = job.PostedAt;
        existing.LastSeenAt = job.LastSeenAt;
        existing.ExpiryDate = job.ExpiryDate;
        existing.VersionHash = job.VersionHash;
        existing.SourceExtraJson = job.SourceExtraJson;
        existing.Status = job.Status;

        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task TouchLastSeenAsync(string sourceName, string externalId, DateTimeOffset seenAt, CancellationToken ct) =>
        await _db.JobPostings
            .Where(x => x.SourceName == sourceName && x.ExternalId == externalId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastSeenAt, seenAt), ct);
}
