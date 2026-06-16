using JobScanner.Application.Scoring;

namespace JobScanner.UnitTests;

public sealed class ScoringEngineTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);
    private readonly ScoringEngine _sut = new(new FakeClock(Now));

    [Fact]
    public void Fresh_job_scores_near_max_freshness()
    {
        var job = TestFactory.Job(postedAt: Now);
        var score = _sut.Score(job, TestFactory.Facts(), TestFactory.Profile());
        var freshness = score.Breakdown.Single(b => b.Criterion == "Freshness").Contribution;
        Assert.True(freshness > 1.9, $"freshness={freshness}");
    }

    [Fact]
    public void Old_job_scores_low_freshness()
    {
        var job = TestFactory.Job(postedAt: Now.AddDays(-21)); // 3 yarı-ömür
        var score = _sut.Score(job, TestFactory.Facts(), TestFactory.Profile());
        var freshness = score.Breakdown.Single(b => b.Criterion == "Freshness").Contribution;
        Assert.True(freshness < 0.3, $"freshness={freshness}");
    }

    [Fact]
    public void No_timezone_requirement_gives_full_fit()
    {
        var score = _sut.Score(TestFactory.Job(postedAt: Now), TestFactory.Facts(timezoneRequirementRaw: null), TestFactory.Profile());
        var tz = score.Breakdown.Single(b => b.Criterion.StartsWith("TimezoneFit")).Contribution;
        Assert.Equal(3.0, tz);
    }

    [Fact]
    public void Same_offset_as_residence_gives_full_fit()
    {
        var score = _sut.Score(TestFactory.Job(postedAt: Now), TestFactory.Facts(timezoneRequirementRaw: "UTC+3 core hours"), TestFactory.Profile());
        var tz = score.Breakdown.Single(b => b.Criterion.StartsWith("TimezoneFit")).Contribution;
        Assert.Equal(3.0, tz);
    }

    [Fact]
    public void Distant_timezone_gives_zero_fit()
    {
        // PST = UTC-8, ikamet UTC+3 → Δ11s, tol 4 → 0
        var score = _sut.Score(TestFactory.Job(postedAt: Now), TestFactory.Facts(timezoneRequirementRaw: "PST 9-5"), TestFactory.Profile());
        var tz = score.Breakdown.Single(b => b.Criterion.StartsWith("TimezoneFit")).Contribution;
        Assert.Equal(0.0, tz);
    }

    [Fact]
    public void Required_keyword_in_title_boosts_match()
    {
        var job = TestFactory.Job(title: "Senior .NET Developer", description: "blah");
        var score = _sut.Score(job, TestFactory.Facts(), TestFactory.Profile(required: [".net"]));
        var match = score.Breakdown.Single(b => b.Criterion.StartsWith("MatchPercent")).Contribution;
        Assert.True(match >= 5.0, $"match={match}"); // req tam → 1.0*5
    }

    [Fact]
    public void Missing_required_keyword_lowers_match()
    {
        var job = TestFactory.Job(title: "Java Developer", description: "Spring");
        var score = _sut.Score(job, TestFactory.Facts(), TestFactory.Profile(required: [".net", "c#"]));
        var match = score.Breakdown.Single(b => b.Criterion.StartsWith("MatchPercent")).Contribution;
        Assert.Equal(0.0, match);
    }

    [Fact]
    public void Final_score_is_clamped_between_0_and_10()
    {
        var job = TestFactory.Job(title: "Senior .NET C# Developer", description: "react azure aws", postedAt: Now);
        var score = _sut.Score(job, TestFactory.Facts(timezoneRequirementRaw: "UTC+3"),
            TestFactory.Profile(required: [".net", "c#"], nice: ["react", "azure", "aws"]));
        Assert.InRange(score.Final, 0, 10);
    }
}
