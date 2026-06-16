using JobScanner.Domain.Enums;

namespace JobScanner.Domain.Jobs;

/// <summary>
/// Normalize edilmiş, kalıcılaştırılabilir ilan. Kimlik = (SourceName, ExternalId) + IdentityKey;
/// içerik değişimi VersionHash ile yakalanır.
/// Kimlik alanları init-only; içerik alanları (değişimde güncellenir) settable.
/// </summary>
public sealed class JobPosting
{
    public long Id { get; init; }                            // surrogate PK
    public required string SourceName { get; init; }
    public required string ExternalId { get; init; }
    public required string IdentityKey { get; init; }        // normalize bileşik anahtar (çapraz-kaynak dedup)
    public DateTimeOffset FirstSeenAt { get; init; }

    public required string Title { get; set; }
    public required string Company { get; set; }
    public required string DescriptionText { get; set; }     // HTML temizlenmiş
    public required string Url { get; set; }
    public string? ApplyUrl { get; set; }                    // yönlendirme hedefi (varsa)
    public WorkMode WorkMode { get; set; } = WorkMode.Remote;
    public DateTimeOffset? PostedAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public DateTimeOffset? ExpiryDate { get; set; }
    public required string VersionHash { get; set; }         // içerik değişimi tespiti
    public string SourceExtraJson { get; set; } = "{}";      // jsonb — heterojen API alanları
    public JobStatus Status { get; set; } = JobStatus.Active;
}
