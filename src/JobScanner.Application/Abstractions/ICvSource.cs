namespace JobScanner.Application.Abstractions;

/// <summary>Kullanıcının ana CV'sinin markdown metni + içerik hash'i (tazeleme anahtarı).</summary>
public sealed record CvDocument(string Markdown, string Hash);

/// <summary>Ana (kaynak) CV'yi sağlar. Implementasyon Infrastructure'da (örn. data/cv.md dosyası).</summary>
public interface ICvSource
{
    /// <summary>Ana CV'yi döndürür; yoksa (dosya bulunamadı/boş) null.</summary>
    Task<CvDocument?> GetAsync(CancellationToken ct);
}
