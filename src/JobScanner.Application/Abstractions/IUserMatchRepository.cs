using JobScanner.Domain.Enums;
using JobScanner.Domain.Matching;

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
        LegitimacyConfidence legitimacy,
        string legitimacySignalsJson,
        CancellationToken ct);

    /// <summary>Arşivlenmiş ilanlara ait açık eşleşmeleri (New/Saved/Opened) Expired yapar; sayısını döner.</summary>
    Task<int> ExpireOpenMatchesForArchivedJobsAsync(CancellationToken ct);

    /// <summary>Okuma: skora göre sıralı eşleşmeler (Expired/Dismissed hariç). source: kaynak SourceName ile filtrele (null=hepsi).</summary>
    Task<IReadOnlyList<MatchView>> GetRankedAsync(long? profileId, double minScore, int take, string? source, CancellationToken ct);

    /// <summary>
    /// Eşleşmeyi yükler, durum makinesi mutasyonunu uygular ve kaydeder. Saf C# domain
    /// metodu (Save/Open/Apply/Dismiss) caller tarafından aktarılır — repo'nun durum
    /// makinesinden haberi olmaz. true: bulundu+kaydedildi; false: eşleşme yok.
    /// </summary>
    Task<bool> WithMatchAsync(long profileId, long jobId, Action<UserJobMatch> mutate, CancellationToken ct);
}
