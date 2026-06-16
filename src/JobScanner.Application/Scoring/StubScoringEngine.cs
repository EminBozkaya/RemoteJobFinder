using JobScanner.Application.Abstractions;
using JobScanner.Domain.Eligibility;
using JobScanner.Domain.Jobs;
using JobScanner.Domain.Users;

namespace JobScanner.Application.Scoring;

/// <summary>
/// Faz 1 STUB: yalnız tazelik (freshness) bileşenini hesaplar. Tam formül
/// (TimezoneFit + MatchPercent) Faz 2'de gerçek facts ile gelir.
/// </summary>
public sealed class StubScoringEngine : IScoringEngine
{
    private const double HalfLifeDays = 7.0;
    private readonly TimeProvider _clock;

    public StubScoringEngine(TimeProvider clock) => _clock = clock;

    public JobScore Score(JobPosting job, EligibilityFacts facts, CriteriaProfile profile)
    {
        var freshness = Freshness(job.PostedAt ?? job.FirstSeenAt);
        var breakdown = new[] { new ScoreContribution("Freshness", freshness) };
        return new JobScore(Math.Clamp(freshness, 0, 10), breakdown);
    }

    private double Freshness(DateTimeOffset postedAt)
    {
        var ageDays = Math.Max(0, (_clock.GetUtcNow() - postedAt).TotalDays);
        // 0-2 aralığı, ~7 günlük yarı-ömür
        return 2.0 * Math.Pow(0.5, ageDays / HalfLifeDays);
    }
}
