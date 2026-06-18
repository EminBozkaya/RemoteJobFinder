namespace JobScanner.Infrastructure.Sources.Remotive;

/// <summary>
/// Remotive kaynak ayarlari. ToS: 24h gecikmeli data, max ~4 cagri/gun onerisi.
/// Bizim 8h interval'imiz uygun (3 cagri/gun).
/// </summary>
public sealed class RemotiveOptions
{
    public const string SectionName = "Sources:Remotive";

    public bool Enabled { get; init; } = true;
    public string BaseUrl { get; init; } = "https://remotive.com/api/remote-jobs";

    /// <summary>Bir 'search' istegi basina maks ilan (1-50; Remotive limit param destekler).</summary>
    public int LimitPerTag { get; init; } = 50;

    /// <summary>Etiket yoksa kullanilacak kategori (bos ise tum kategoriler).</summary>
    public string? Category { get; init; }
}
