using JobScanner.Application.Abstractions;
using JobScanner.Domain.Eligibility;
using JobScanner.Domain.Enums;
using JobScanner.Domain.Jobs;
using JobScanner.Domain.Users;

namespace JobScanner.Application.Scoring;

/// <summary>
/// Saf C# puanlama: SkillFit*5 + TimezoneFit(0-3) + Freshness(0-2) + EngagementFit(0-1)
/// + ExperienceFit(-2..0), 0-10'a clamp. Faz 5b: keyword yerine öz-puanlı yetkinlikler;
/// ilanın istediği tecrübe yılı eksikse yumuşak ceza. Açıklanabilir döküm üretir; ML yok.
/// </summary>
public sealed class ScoringEngine : IScoringEngine
{
    private const double HalfLifeDays = 7.0;
    private const double ResidenceUtcOffset = 3.0; // Türkiye, UTC+3
    private const double TitleWeight = 1.0;
    private const double BodyWeight = 0.6;

    private readonly TimeProvider _clock;

    public ScoringEngine(TimeProvider clock) => _clock = clock;

    public JobScore Score(JobPosting job, EligibilityFacts facts, CriteriaProfile profile)
    {
        var skillFit = SkillFit(job, profile, out var skillDetail);
        var timezoneFit = TimezoneFit(facts.TimezoneRequirementRaw, profile.TimezoneToleranceHours, out var tzDetail);
        var freshness = Freshness(job.PostedAt ?? job.FirstSeenAt);
        var engagementFit = EngagementFit(facts, out var engDetail);
        var experienceFit = ExperienceFit(facts.RequiredExperience, profile, out var expDetail);

        var raw = skillFit * 5.0 + timezoneFit + freshness + engagementFit + experienceFit;
        var final = Math.Clamp(raw, 0, 10);

        var breakdown = new[]
        {
            new ScoreContribution($"SkillFit ({skillDetail})", Math.Round(skillFit * 5.0, 2)),
            new ScoreContribution($"TimezoneFit ({tzDetail})", Math.Round(timezoneFit, 2)),
            new ScoreContribution("Freshness", Math.Round(freshness, 2)),
            new ScoreContribution($"EngagementFit ({engDetail})", Math.Round(engagementFit, 2)),
            new ScoreContribution($"ExperienceFit ({expDetail})", Math.Round(experienceFit, 2)),
        };

        return new JobScore(Math.Round(final, 2), breakdown);
    }

    /// <summary>0-1: kullanıcının yetkinliklerinin ilanda görünmesi, öz-puanla (1-10) ağırlıklı.</summary>
    private static double SkillFit(JobPosting job, CriteriaProfile profile, out string detail)
    {
        if (profile.Skills.Count == 0)
        {
            detail = "kısıt yok";
            return 1.0;
        }

        double weightSum = 0, scoreSum = 0;
        foreach (var s in profile.Skills)
        {
            var w = Math.Clamp(s.SelfRating, 1, 10) / 10.0;
            weightSum += w;
            scoreSum += w * Presence(s.Name, job.Title, job.DescriptionText);
        }

        if (weightSum == 0) { detail = "kısıt yok"; return 1.0; }
        var pct = scoreSum / weightSum;
        detail = $"{profile.Skills.Count} yetkinlik={pct:0.00}";
        return pct;
    }

    /// <summary>-2..0: ilan "X için min N yıl" istiyor ve kullanıcının yılı azsa yumuşak ceza (eleme değil).</summary>
    private static double ExperienceFit(IReadOnlyList<SkillRequirement>? required, CriteriaProfile profile, out string detail)
    {
        if (required is null || required.Count == 0 || profile.Skills.Count == 0)
        {
            detail = "-";
            return 0.0;
        }

        double penalty = 0;
        var gaps = new List<string>();
        foreach (var req in required)
        {
            var mine = profile.Skills.FirstOrDefault(s => string.Equals(s.Name, req.Skill, StringComparison.OrdinalIgnoreCase));
            if (mine is null) continue; // sahip olmadığım yetkinlik SkillFit'i zaten düşürüyor
            if (mine.Years < req.MinYears)
            {
                penalty += Math.Clamp((req.MinYears - mine.Years) * 0.25, 0, 1);
                gaps.Add($"{req.Skill} {mine.Years}/{req.MinYears}y");
            }
        }

        detail = gaps.Count == 0 ? "tam" : string.Join(", ", gaps);
        return -Math.Min(penalty, 2.0);
    }

    /// <summary>
    /// 0-1: çalışma türü tercihi. Kullanıcının şirketi yok → EOR (TR'de yasal çalışan) en değerli;
    /// B2B/contractor uygun ama şirket/fatura masrafı gerektirir; diğerleri nötr.
    /// </summary>
    private static double EngagementFit(EligibilityFacts facts, out string detail)
    {
        if (facts.MentionsEor == true || facts.EngagementType == EngagementType.EmployeeViaEor)
        {
            detail = "EOR (ideal)";
            return 1.0;
        }
        if (facts.EngagementType is EngagementType.Contractor or EngagementType.B2B or EngagementType.Freelance)
        {
            detail = $"{facts.EngagementType} (şirket gerekir)";
            return 0.4;
        }
        detail = "nötr";
        return 0.0;
    }

    /// <summary>Başlıkta tam, gövdede kısmi kredi; bulunmazsa 0.</summary>
    private static double Presence(string keyword, string title, string body)
    {
        if (Contains(title, keyword)) return TitleWeight;
        if (Contains(body, keyword)) return BodyWeight;
        return 0.0;
    }

    /// <summary>0-3: ikamet (UTC+3) ofsetine fark, kullanıcı toleransına göre.</summary>
    private static double TimezoneFit(string? requirementRaw, int toleranceHours, out string detail)
    {
        if (string.IsNullOrWhiteSpace(requirementRaw))
        {
            detail = "kısıt yok";
            return 3.0;
        }

        var offset = TimezoneParser.TryParseUtcOffset(requirementRaw);
        if (offset is null)
        {
            detail = "çözülemedi";
            return 1.5; // gereksinim var ama belirsiz → nötr
        }

        var diff = Math.Abs(offset.Value - ResidenceUtcOffset);
        detail = $"Δ{diff:0.#}s, tol={toleranceHours}";
        if (diff <= toleranceHours) return 3.0;
        if (diff <= toleranceHours + 2) return 1.5;
        return 0.0;
    }

    /// <summary>0-2: ~7 günlük yarı-ömür.</summary>
    private double Freshness(DateTimeOffset postedAt)
    {
        var ageDays = Math.Max(0, (_clock.GetUtcNow() - postedAt).TotalDays);
        return 2.0 * Math.Pow(0.5, ageDays / HalfLifeDays);
    }

    private static bool Contains(string haystack, string needle) =>
        !string.IsNullOrWhiteSpace(needle) && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
