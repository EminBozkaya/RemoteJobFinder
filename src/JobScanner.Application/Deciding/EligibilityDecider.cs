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

        // --- Sert eleyiciler (PLAN §6) ---
        if (facts.RequiresCitizenship == true)
            return Ineligible($"Vatandaşlık şartı var ({residence} karşılamıyor)");

        if (facts.RequiresWorkAuth == true && !allowsResidence)
            return Ineligible($"Yerel çalışma izni şartı ve {residence} izinli değil");

        if (allowedCountries.Count > 0 && !isGlobal && !allowsResidence)
            return Ineligible($"İzinli ülkeler {residence} içermiyor: [{string.Join(", ", allowedCountries)}]");

        if (Geography.MentionsEuBoundary(facts.DataBoundary) && !Geography.IsEuResidence(residence))
            return Ineligible($"Veri sınırı AB/AEA ({residence} dışında): '{facts.DataBoundary}'");

        // --- EOR düzeltmesi (YZ4): EOR tek başına eleyici DEĞİL ---
        if (facts.MentionsEor == true)
            reasons.Add(facts.EngagementType == EngagementType.EmployeeViaEor
                ? $"EOR üzerinden istihdam (bilgi){FormatPlatform(facts.EorPlatform)}"
                : $"EOR'dan bahsediyor ama eleyici değil{FormatPlatform(facts.EorPlatform)}");

        if (facts.EngagementType is EngagementType.Contractor or EngagementType.B2B or EngagementType.Freelance)
            reasons.Add($"Uygun çalışma türü: {facts.EngagementType}");

        // --- Belirsizlik: düşük güven veya çelişki ---
        if (facts.Confidence < _options.MinConfidence)
            return Uncertain(reasons, $"Düşük güven ({facts.Confidence:0.00} < {_options.MinConfidence:0.00})");

        if (facts.RequiresWorkAuth == true && allowsResidence && facts.AllowedCountries is { Count: > 0 })
            reasons.Add($"Çalışma izni şartı var ama {residence} izinli görünüyor");

        // Çelişki: B2B/contractor isteyen profil ama ilan açıkça contractor kabul etmiyor
        if (facts.AllowsB2BContractor == false && WantsContractor(profile))
            return Uncertain(reasons, "İlan B2B/contractor kabul etmiyor ama profil bunu istiyor");

        reasons.Add($"Sert eleyici yok; {residence} için uygun görünüyor");
        return (Decision.Eligible, reasons);
    }

    private static bool WantsContractor(CriteriaProfile p) =>
        p.ContractTypes.Any(t => t.Contains("b2b", StringComparison.OrdinalIgnoreCase) ||
                                 t.Contains("contractor", StringComparison.OrdinalIgnoreCase) ||
                                 t.Contains("freelance", StringComparison.OrdinalIgnoreCase));

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
