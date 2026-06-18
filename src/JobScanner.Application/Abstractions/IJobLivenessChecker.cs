namespace JobScanner.Application.Abstractions;

/// <summary>
/// Bir ilan URL'sinin hâlâ canlı (200/3xx) olup olmadığını kontrol eder.
/// Belirsiz durumlar (timeout, 5xx, bloklama) IsAlive=null döner — pipeline bu
/// durumlarda ihtiyatlı davranıp ilanı işlemeye devam eder.
/// </summary>
public interface IJobLivenessChecker
{
    /// <returns>true=200/3xx, false=404/410/Gone, null=karar verilemedi (timeout/5xx/blok)</returns>
    Task<bool?> IsAliveAsync(string url, CancellationToken ct);
}
