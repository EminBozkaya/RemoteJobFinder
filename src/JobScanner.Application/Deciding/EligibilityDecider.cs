using JobScanner.Application.Abstractions;
using JobScanner.Domain.Eligibility;
using JobScanner.Domain.Enums;
using JobScanner.Domain.Users;
using Microsoft.Extensions.Options;

namespace JobScanner.Application.Deciding;

/// <summary>
/// Saf C# uygunluk kararı (PLAN §6). Sert eleyiciler → Ineligible; EOR düzeltmesi uygulanır;
/// düşük güven/çelişki → Uncertain; aksi halde Eligible. Token harcamaz.
/// </summary>
public sealed class EligibilityDecider : IEligibilityDecider
{
    private readonly DeciderOptions _options;

    public EligibilityDecider(IOptions<DeciderOptions> options) => _options = options.Value;

    public (Decision Decision, IReadOnlyList<string> Reasons) Decide(EligibilityFacts facts, CriteriaProfile profile)
    {
        var reasons = new List<string>();
        var residence = profile.ResidenceCountry;

        var allowedCountries = facts.AllowedCountries ?? [];
        var isGlobal = allowedCountries.Count > 0 && Geography.IsGlobal(allowedCountries);
        var allowsResidence = isGlobal || (allowedCountries.Count > 0 && Geography.Contains(allowedCountries, residence));

        // --- Sert eleyiciler: TR'den yasal remote çalışmayı engelleyenler (PLAN §6) ---
        // Çalışma TÜRÜ (B2B/employee/EOR) eleyici DEĞİL; önemli olan coğrafya + izin + taşınma.
        if (facts.RequiresCitizenship == true)
            return Ineligible($"Vatandaşlık şartı var ({residence} karşılamıyor)");

        if (facts.RequiresRelocation == true)
            return Ineligible("Taşınma/onsite'a geçiş şartı var (TR'den çalışmaya uygun değil)");

        if (RequiresForeignBackgroundCheck(facts, residence))
            return Ineligible($"'{facts.BackgroundCheckCountry}' ülkesine özgü adli sicil/clearance şartı; {residence}'den karşılanamaz");

        if (facts.RequiresWorkAuth == true && !allowsResidence)
            return Ineligible($"Yerel çalışma izni şartı ve {residence} izinli değil");

        if (allowedCountries.Count > 0 && !isGlobal && !allowsResidence)
            return Ineligible($"İzinli ülkeler {residence} içermiyor: [{string.Join(", ", allowedCountries)}]");

        if (Geography.MentionsEuBoundary(facts.DataBoundary) && !Geography.IsEuResidence(residence))
            return Ineligible($"Veri sınırı AB/AEA ({residence} dışında): '{facts.DataBoundary}'");

        // --- EOR pozitif sinyal: TR'de yasal çalışan olmayı sağlar (eleyici değil, tercih edilir) ---
        if (IsEorFriendly(facts))
            reasons.Add($"EOR üzerinden yasal istihdam mümkün{FormatPlatform(facts.EorPlatform)}");

        if (facts.EngagementType is EngagementType.Contractor or EngagementType.B2B or EngagementType.Freelance)
            reasons.Add($"Çalışma türü: {facts.EngagementType} (uygun)");

        // --- Belirsizlik: yalnız düşük güven ---
        if (facts.Confidence < _options.MinConfidence)
            return Uncertain(reasons, $"Düşük güven ({facts.Confidence:0.00} < {_options.MinConfidence:0.00})");

        if (facts.RequiresWorkAuth == true && allowsResidence && facts.AllowedCountries is { Count: > 0 })
            reasons.Add($"Çalışma izni şartı var ama {residence} izinli görünüyor");

        reasons.Add($"Sert eleyici yok; {residence} için uygun görünüyor");
        return (Decision.Eligible, reasons);
    }

    /// <summary>
    /// İlan, ikamet ülkesi DIŞINDA bir ülkeye özgü adli sicil/clearance istiyor mu?
    /// (ör. TR'li aday için "UK DBS check"). Genel/ülkesiz background check eleyici değildir
    /// (kendi ülkenin temiz sicilini sunabilirsin).
    /// </summary>
    internal static bool RequiresForeignBackgroundCheck(EligibilityFacts f, string residence)
    {
        if (string.IsNullOrWhiteSpace(f.BackgroundCheckCountry)) return false;
        var country = new[] { f.BackgroundCheckCountry };
        return !Geography.IsGlobal(country) && !Geography.Contains(country, residence);
    }

    /// <summary>EOR ile çalışma mümkün mü (TR'de yasal çalışan olmayı sağlar — tercih edilir).</summary>
    internal static bool IsEorFriendly(EligibilityFacts f) =>
        f.MentionsEor == true || f.EngagementType == EngagementType.EmployeeViaEor;

    private static string FormatPlatform(string? platform) =>
        string.IsNullOrWhiteSpace(platform) ? string.Empty : $" — {platform}";

    private static (Decision, IReadOnlyList<string>) Ineligible(string reason) =>
        (Decision.Ineligible, [reason]);

    private static (Decision, IReadOnlyList<string>) Uncertain(List<string> reasons, string reason)
    {
        reasons.Add(reason);
        return (Decision.Uncertain, reasons);
    }
}
