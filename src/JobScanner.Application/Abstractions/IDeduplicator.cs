using JobScanner.Domain.Jobs;

namespace JobScanner.Application.Abstractions;

/// <summary>İlanı New / Unchanged / Changed olarak sınıflar (kimlik + VersionHash).</summary>
public interface IDeduplicator
{
    Task<DedupResult> ClassifyAsync(JobPosting job, CancellationToken ct);
}
