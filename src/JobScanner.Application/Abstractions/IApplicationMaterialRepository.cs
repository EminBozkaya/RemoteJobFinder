using JobScanner.Domain.Applications;

namespace JobScanner.Application.Abstractions;

/// <summary>Üretilmiş başvuru materyalinin kalıcılığı. Anahtar = (ProfileId, JobId).</summary>
public interface IApplicationMaterialRepository
{
    Task<ApplicationMaterial?> GetAsync(long profileId, long jobId, CancellationToken ct);

    /// <summary>Ekler veya günceller (upsert).</summary>
    Task SaveAsync(ApplicationMaterial material, CancellationToken ct);
}
