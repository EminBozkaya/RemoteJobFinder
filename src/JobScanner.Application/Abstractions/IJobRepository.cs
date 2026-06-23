using JobScanner.Domain.Jobs;

namespace JobScanner.Application.Abstractions;

/// <summary>İlan kalıcılığı. Kimlik = (SourceName, ExternalId).</summary>
public interface IJobRepository
{
    /// <summary>Var olan kaydı bulur (kimlik ile). Yoksa null.</summary>
    Task<JobPosting?> FindByIdentityAsync(string sourceName, string externalId, CancellationToken ct);

    /// <summary>Surrogate Id ile bulur. Yoksa null.</summary>
    Task<JobPosting?> FindByIdAsync(long id, CancellationToken ct);

    /// <summary>Tüm aktif (arşivlenmemiş) ilanlar — kriter değişince yeniden hesaplama için.</summary>
    Task<IReadOnlyList<JobPosting>> GetActiveAsync(CancellationToken ct);

    /// <summary>Ekler veya günceller; kalıcı Id'si atanmış ilanı döner.</summary>
    Task<JobPosting> UpsertAsync(JobPosting job, CancellationToken ct);

    /// <summary>İçerik değişmemişse yalnız LastSeenAt'i günceller (pahalı işi atlar).</summary>
    Task TouchLastSeenAsync(string sourceName, string externalId, DateTimeOffset seenAt, CancellationToken ct);

    /// <summary>Belirtilen tarihten beri görülmeyen aktif ilanları arşivler; arşivlenen sayısını döner.</summary>
    Task<int> ArchiveStaleAsync(DateTimeOffset notSeenSince, CancellationToken ct);

    /// <summary>Tek bir ilanı arşivler (ör. liveness gate 404 saptadığında).</summary>
    Task ArchiveOneAsync(long jobId, CancellationToken ct);
}
