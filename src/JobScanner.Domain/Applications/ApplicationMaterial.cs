namespace JobScanner.Domain.Applications;

/// <summary>
/// Bir (Profil, İlan) çifti için üretilmiş ve kalıcılaştırılmış başvuru materyali.
/// Üretim pahalı (LLM) olduğu için saklanır; tazeleme anahtarı = SourceCvHash + PromptVersion +
/// ModelVersion + JobVersionHash. Bunlardan biri değişirse materyal bayatlamıştır, yeniden üretilir.
/// </summary>
public sealed class ApplicationMaterial
{
    public required long ProfileId { get; init; }
    public required long JobId { get; init; }

    public required string CoverLetter { get; set; }
    public required string TailoredCvMarkdown { get; set; }
    public required string Language { get; set; }

    // Tazeleme (cache invalidation) bileşenleri
    public required string SourceCvHash { get; set; }
    public required string PromptVersion { get; set; }
    public required string ModelVersion { get; set; }
    public required string JobVersionHash { get; set; }

    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>Saklı materyal verilen anahtarlarla hâlâ taze mi?</summary>
    public bool IsFreshFor(string sourceCvHash, string promptVersion, string modelVersion, string jobVersionHash) =>
        SourceCvHash == sourceCvHash
        && PromptVersion == promptVersion
        && ModelVersion == modelVersion
        && JobVersionHash == jobVersionHash;
}
