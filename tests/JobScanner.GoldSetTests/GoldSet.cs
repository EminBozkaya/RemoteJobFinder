using JobScanner.Domain.Eligibility;
using JobScanner.Domain.Enums;
using JobScanner.Domain.Users;

namespace JobScanner.GoldSetTests;

/// <summary>
/// ~20 etiketli gerçek-dünya senaryosu (uygun / kesin elenmeli / belirsiz). Karar mantığı
/// (IEligibilityDecider) regresyonunu kilitler; prompt/model değişse de kararlar bozulmamalı.
/// </summary>
public sealed record GoldCase(string Name, EligibilityFacts Facts, Decision Expected);

internal static class GoldSet
{
    public static readonly CriteriaProfile TrContractorProfile = new()
    {
        Id = 1,
        UserId = 1,
        Name = "TR B2B Contractor",
        ResidenceCountry = "TR",
        ContractTypes = ["b2b", "contractor"],
        TimezoneToleranceHours = 4,
    };

    public static IEnumerable<GoldCase> Cases()
    {
        yield return new("worldwide-contractor", F(allowed: ["Worldwide"], eng: EngagementType.Contractor, conf: 0.9), Decision.Eligible);
        yield return new("anywhere-b2b", F(allowed: ["Anywhere"], eng: EngagementType.B2B, conf: 0.85), Decision.Eligible);
        yield return new("turkey-allowed", F(allowed: ["Turkey", "Germany"], conf: 0.8), Decision.Eligible);
        yield return new("europe-and-turkey", F(allowed: ["Europe", "Turkey"], conf: 0.7, tz: "CET overlap"), Decision.Eligible);
        yield return new("eor-mention-but-contractor", F(eor: true, platform: "Deel", eng: EngagementType.Contractor, conf: 0.8), Decision.Eligible);
        yield return new("employee-via-eor", F(eor: true, eng: EngagementType.EmployeeViaEor, conf: 0.75), Decision.Eligible);
        yield return new("no-constraints-high-conf", F(conf: 0.9), Decision.Eligible);
        yield return new("freelance-global", F(allowed: ["Global"], eng: EngagementType.Freelance, conf: 0.8), Decision.Eligible);
        yield return new("plain-employee-tr-allowed", F(allowed: ["Turkey"], eng: EngagementType.Employee, conf: 0.9), Decision.Eligible);
        yield return new("b2b-not-mentioned-still-eligible", F(allowed: ["Worldwide"], allowsB2B: null, conf: 0.85), Decision.Eligible);
        yield return new("generic-background-check-ok", F(allowed: ["Worldwide"], conf: 0.85), Decision.Eligible); // ülkesiz background check elemez
        yield return new("turkey-background-check-ok", F(bgCheckCountry: "Turkey", allowed: ["Worldwide"], conf: 0.9), Decision.Eligible);

        // Kesin elenmeli
        yield return new("us-only-work-auth", F(workAuth: true, allowed: ["United States"], conf: 0.9), Decision.Ineligible);
        yield return new("citizenship-required", F(citizenship: true, conf: 0.9), Decision.Ineligible);
        yield return new("us-only-list", F(allowed: ["USA"], conf: 0.85), Decision.Ineligible);
        yield return new("eu-data-boundary", F(dataBoundary: "EU only (GDPR)", conf: 0.9), Decision.Ineligible);
        yield return new("eea-residency", F(dataBoundary: "EEA residency required", conf: 0.8), Decision.Ineligible);
        yield return new("uk-canada-only", F(allowed: ["UK", "Canada"], conf: 0.8), Decision.Ineligible);
        yield return new("work-auth-no-country-info", F(workAuth: true, conf: 0.85), Decision.Ineligible);
        yield return new("us-citizen-clearance", F(citizenship: true, workAuth: true, allowed: ["USA"], conf: 0.95), Decision.Ineligible);
        yield return new("relocation-required", F(relocation: true, allowed: ["Worldwide"], conf: 0.9), Decision.Ineligible);
        yield return new("relocation-required-eor-cant-save", F(relocation: true, eor: true, conf: 0.9), Decision.Ineligible);
        yield return new("uk-background-check", F(bgCheckCountry: "UK", allowed: ["Worldwide"], conf: 0.9), Decision.Ineligible);
        yield return new("us-clearance-by-country", F(bgCheckCountry: "US", conf: 0.9), Decision.Ineligible);

        // Belirsiz
        yield return new("low-confidence", F(allowed: ["Worldwide"], conf: 0.2), Decision.Uncertain);
        yield return new("b2b-rejected-not-a-gate", F(allowsB2B: false, eng: EngagementType.Employee, conf: 0.8), Decision.Eligible); // çalışma türü eleme değil
        yield return new("eu-boundary-low-conf", F(dataBoundary: "EU only", conf: 0.3), Decision.Ineligible); // sert eleme güven kontrolünden önce gelir
        yield return new("empty-facts-zero-conf", F(conf: 0.0), Decision.Uncertain);
    }

    private static EligibilityFacts F(
        bool? workAuth = null,
        bool? relocation = null,
        string? bgCheckCountry = null,
        IReadOnlyList<string>? allowed = null,
        bool? citizenship = null,
        bool? allowsB2B = null,
        EngagementType eng = EngagementType.Unknown,
        bool? eor = null,
        string? platform = null,
        string? dataBoundary = null,
        string? tz = null,
        double conf = 0.8) =>
        new(
            JobId: 1, PromptVersion: "v1", ModelVersion: "gold", VersionHash: "h",
            RequiresWorkAuth: workAuth, RequiresRelocation: relocation,
            BackgroundCheckCountry: bgCheckCountry,
            AllowedCountries: allowed, RequiresCitizenship: citizenship, IsLikelyGhost: null,
            AllowsB2BContractor: allowsB2B, EngagementType: eng, MentionsEor: eor, EorPlatform: platform,
            DataBoundary: dataBoundary, TimezoneRequirementRaw: tz, IsRecruiterAgency: null,
            Confidence: conf, ExtractedAt: DateTimeOffset.UnixEpoch, RawJson: "{}");
}
