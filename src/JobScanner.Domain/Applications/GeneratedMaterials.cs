namespace JobScanner.Domain.Applications;

/// <summary>
/// LLM'in bir ilan için ürettiği başvuru materyali (yan etkisiz değer nesnesi).
/// Sistem bunu GÖNDERMEZ; yalnız hazırlar. Dil ilan diline uydurulur (örn. "en", "tr").
/// </summary>
public sealed record GeneratedMaterials(
    string CoverLetter,
    string TailoredCvMarkdown,
    string Language);
