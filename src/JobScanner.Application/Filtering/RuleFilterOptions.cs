namespace JobScanner.Application.Filtering;

/// <summary>Tipli, config-driven kural elemesi ayarları (profil-bağımsız, ucuz kademe).</summary>
public sealed class RuleFilterOptions
{
    public const string SectionName = "RuleFilter";

    /// <summary>Başlık/açıklamada geçerse ilan elenir (case-insensitive).</summary>
    public IReadOnlyList<string> ForbiddenKeywords { get; init; } = [];

    /// <summary>En az biri başlık/açıklamada geçmeli; hiçbiri yoksa elenir. Boşsa kontrol atlanır.</summary>
    public IReadOnlyList<string> RequiredKeywords { get; init; } = [];
}
