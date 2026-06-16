using JobScanner.Application.Abstractions;
using JobScanner.Domain.Enums;
using JobScanner.Domain.Jobs;
using Microsoft.Extensions.Options;

namespace JobScanner.Application.Filtering;

/// <summary>
/// Ucuz, tipli, yan etkisiz kural elemesi. Token harcamadan net elemeleri yapar:
/// WorkMode != Remote, yasaklı keyword, gerekli keyword'lerin hiçbirinin olmaması.
/// </summary>
public sealed class RuleFilter : IRuleFilter
{
    private readonly IReadOnlyList<string> _forbidden;
    private readonly IReadOnlyList<string> _required;

    public RuleFilter(IOptions<RuleFilterOptions> options)
    {
        _forbidden = options.Value.ForbiddenKeywords;
        _required = options.Value.RequiredKeywords;
    }

    public RuleResult Evaluate(JobPosting job)
    {
        if (job.WorkMode != WorkMode.Remote)
            return RuleResult.Eliminate($"WorkMode '{job.WorkMode}' tam remote değil");

        var haystack = $"{job.Title}\n{job.DescriptionText}";

        foreach (var forbidden in _forbidden)
        {
            if (Contains(haystack, forbidden))
                return RuleResult.Eliminate($"Yasaklı keyword: '{forbidden}'");
        }

        if (_required.Count > 0 && !_required.Any(k => Contains(haystack, k)))
            return RuleResult.Eliminate("Gerekli keyword'lerin hiçbiri yok");

        return RuleResult.Pass();
    }

    private static bool Contains(string haystack, string needle) =>
        !string.IsNullOrWhiteSpace(needle) &&
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
