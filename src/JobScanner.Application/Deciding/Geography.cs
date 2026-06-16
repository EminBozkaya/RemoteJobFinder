namespace JobScanner.Application.Deciding;

/// <summary>
/// Coğrafi eşleştirme yardımcıları (saf). LLM'in çıkardığı serbest-metin ülke/bölge
/// ifadelerini ikamet ülkesiyle karşılaştırır.
/// </summary>
internal static class Geography
{
    // "Her yerden" anlamına gelen ifadeler — AllowedCountries bunları içeriyorsa kısıt yok sayılır.
    private static readonly string[] GlobalTokens =
        ["worldwide", "anywhere", "global", "globally", "any country", "everywhere", "international"];

    // Basit AB/AEA tespiti için anahtarlar (DataBoundary serbest metni).
    private static readonly string[] EuBoundaryTokens =
        ["eu", "e.u", "eea", "european union", "europe-only", "gdpr", "european economic area"];

    // İkamet ülkesi takma adları (kod + yaygın adlar). Genişletilebilir.
    private static readonly Dictionary<string, string[]> CountryAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TR"] = ["tr", "tur", "turkey", "türkiye", "turkiye"],
    };

    private static readonly HashSet<string> EuCountryCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "AT", "BE", "BG", "HR", "CY", "CZ", "DK", "EE", "FI", "FR", "DE", "GR", "HU", "IE",
        "IT", "LV", "LT", "LU", "MT", "NL", "PL", "PT", "RO", "SK", "SI", "ES", "SE",
    };

    public static bool IsGlobal(IReadOnlyList<string> countries) =>
        countries.Any(c => GlobalTokens.Any(t => c.Contains(t, StringComparison.OrdinalIgnoreCase)));

    public static bool Contains(IReadOnlyList<string> countries, string residenceCountry)
    {
        var aliases = AliasesFor(residenceCountry);
        foreach (var c in countries)
        {
            var norm = c.Trim();
            if (aliases.Any(a => norm.Equals(a, StringComparison.OrdinalIgnoreCase) ||
                                 norm.Contains(a, StringComparison.OrdinalIgnoreCase)))
                return true;
        }
        return false;
    }

    public static bool MentionsEuBoundary(string? dataBoundary)
    {
        if (string.IsNullOrWhiteSpace(dataBoundary)) return false;
        return EuBoundaryTokens.Any(t => dataBoundary.Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsEuResidence(string residenceCountry) =>
        EuCountryCodes.Contains(residenceCountry.Trim());

    private static string[] AliasesFor(string residenceCountry) =>
        CountryAliases.TryGetValue(residenceCountry.Trim(), out var a)
            ? a
            : [residenceCountry.Trim().ToLowerInvariant()];
}
