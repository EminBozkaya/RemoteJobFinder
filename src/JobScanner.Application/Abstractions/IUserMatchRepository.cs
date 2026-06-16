using JobScanner.Domain.Enums;

namespace JobScanner.Application.Abstractions;

/// <summary>Profil-ilan eşleşmelerinin kalıcılığı + durum makinesi.</summary>
public interface IUserMatchRepository
{
    /// <summary>Eşleşme kapalı mı (Applied/Dismissed) — öyleyse yeniden işlenmez.</summary>
    Task<bool> IsClosedAsync(long profileId, long jobId, CancellationToken ct);

    Task UpsertAsync(
        long profileId,
        long jobId,
        double score,
        string scoreBreakdownJson,
        Decision decision,
        string decisionReasonsJson,
        CancellationToken ct);
}
