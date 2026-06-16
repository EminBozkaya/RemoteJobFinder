using JobScanner.Application.Abstractions;
using JobScanner.Domain.Jobs;

namespace JobScanner.Infrastructure.Persistence;

/// <summary>
/// Ilani New / Unchanged / Changed olarak siniflar. Kimlik (SourceName, ExternalId) ile bulur;
/// icerik degisimini VersionHash ile karsilastirir.
/// </summary>
public sealed class Deduplicator : IDeduplicator
{
    private readonly IJobRepository _jobs;

    public Deduplicator(IJobRepository jobs) => _jobs = jobs;

    public async Task<DedupResult> ClassifyAsync(JobPosting job, CancellationToken ct)
    {
        var existing = await _jobs.FindByIdentityAsync(job.SourceName, job.ExternalId, ct);
        if (existing is null)
            return new DedupResult(DedupKind.New, null);

        return existing.VersionHash == job.VersionHash
            ? new DedupResult(DedupKind.Unchanged, existing)
            : new DedupResult(DedupKind.Changed, existing);
    }
}
