using JobScanner.Application.Abstractions;
using JobScanner.Domain.Enums;
using JobScanner.Domain.Jobs;
using JobScanner.Domain.Users;

namespace JobScanner.Application.Filtering;

/// <summary>
/// Ucuz, tipli, yan etkisiz kural elemesi. Token harcamadan net elemeleri yapar:
/// WorkMode != Remote, ya da profilin yasaklı keyword'lerinden biri başlık/gövdede geçiyorsa.
/// Required keyword'ler eleyici DEĞİL (scoring sinyali); forbidden artık profilden gelir (Faz 5a).
/// </summary>
public sealed class RuleFilter : IRuleFilter
{
    public RuleResult Evaluate(JobPosting job, CriteriaProfile profile)
    {
        if (job.WorkMode != WorkMode.Remote)
            return RuleResult.Eliminate($"WorkMode '{job.WorkMode}' tam remote değil");

        var haystack = $"{job.Title}\n{job.DescriptionText}";

        foreach (var forbidden in profile.ForbiddenKeywords)
        {
            if (Contains(haystack, forbidden))
                return RuleResult.Eliminate($"Yasaklı keyword: '{forbidden}'");
        }

        return RuleResult.Pass();
    }

    private static bool Contains(string haystack, string needle) =>
        !string.IsNullOrWhiteSpace(needle) &&
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
