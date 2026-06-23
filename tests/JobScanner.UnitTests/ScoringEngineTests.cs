using JobScanner.Application.Scoring;
using JobScanner.Domain.Eligibility;
using JobScanner.Domain.Users;

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
    public void Skill_in_title_boosts_skillfit()
    {
        var job = TestFactory.Job(title: "Senior .NET Developer", description: "blah");
        var score = _sut.Score(job, TestFactory.Facts(), TestFactory.Profile(skills: [new SkillCriterion(".net", 10, 5)]));
        var fit = score.Breakdown.Single(b => b.Criterion.StartsWith("SkillFit")).Contribution;
        Assert.True(fit >= 5.0, $"skillfit={fit}"); // başlıkta tam → 1.0*5
    }

    [Fact]
    public void Missing_skill_lowers_skillfit()
    {
        var job = TestFactory.Job(title: "Java Developer", description: "Spring");
        var score = _sut.Score(job, TestFactory.Facts(), TestFactory.Profile(skills: [new SkillCriterion(".net", 10, 5), new SkillCriterion("c#", 10, 5)]));
        var fit = score.Breakdown.Single(b => b.Criterion.StartsWith("SkillFit")).Contribution;
        Assert.Equal(0.0, fit);
    }

    [Fact]
    public void Higher_self_rating_weighs_more()
    {
        // İlanda yalnız "c#" geçiyor. c# öz-puanı yüksekse SkillFit daha yüksek olmalı.
        var job = TestFactory.Job(title: "C# Developer", description: "backend");
        double Fit(int csharpRating) => _sut
            .Score(job, TestFactory.Facts(), TestFactory.Profile(skills: [new SkillCriterion("c#", csharpRating, 5), new SkillCriterion("react", 5, 5)]))
            .Breakdown.Single(b => b.Criterion.StartsWith("SkillFit")).Contribution;

        Assert.True(Fit(10) > Fit(2), "yüksek öz-puanlı c# daha çok ağırlık taşımalı");
    }

    [Fact]
    public void Experience_shortfall_applies_soft_penalty()
    {
        // İlan React min 3 yıl istiyor; kullanıcının 1 yılı var → ExperienceFit negatif (eleme değil).
        var job = TestFactory.Job(title: "React Developer", description: "frontend");
        var facts = TestFactory.Facts(requiredExperience: [new SkillRequirement("React", 3)]);
        var profile = TestFactory.Profile(skills: [new SkillCriterion("React", 8, 1)]);

        var exp = _sut.Score(job, facts, profile).Breakdown.Single(b => b.Criterion.StartsWith("ExperienceFit")).Contribution;
        Assert.True(exp < 0, $"experienceFit={exp}");
    }

    [Fact]
    public void No_experience_penalty_when_years_sufficient()
    {
        var job = TestFactory.Job(title: "React Developer", description: "frontend");
        var facts = TestFactory.Facts(requiredExperience: [new SkillRequirement("React", 3)]);
        var profile = TestFactory.Profile(skills: [new SkillCriterion("React", 8, 5)]); // 5 ≥ 3

        var exp = _sut.Score(job, facts, profile).Breakdown.Single(b => b.Criterion.StartsWith("ExperienceFit")).Contribution;
        Assert.Equal(0.0, exp);
    }

    [Fact]
    public void Eor_ranks_above_b2b_above_neutral()
    {
        var job = TestFactory.Job(postedAt: Now);
        var profile = TestFactory.Profile();

        double Eng(EligibilityFacts f) =>
            _sut.Score(job, f, profile).Breakdown.Single(b => b.Criterion.StartsWith("EngagementFit")).Contribution;

        var eor = Eng(TestFactory.Facts(mentionsEor: true));
        var b2b = Eng(TestFactory.Facts(engagementType: Domain.Enums.EngagementType.B2B));
        var neutral = Eng(TestFactory.Facts(engagementType: Domain.Enums.EngagementType.Employee));

        Assert.True(eor > b2b, $"eor={eor} b2b={b2b}");
        Assert.True(b2b > neutral, $"b2b={b2b} neutral={neutral}");
    }

    [Fact]
    public void Final_score_is_clamped_between_0_and_10()
    {
        var job = TestFactory.Job(title: "Senior .NET C# Developer", description: "react azure aws", postedAt: Now);
        var score = _sut.Score(job, TestFactory.Facts(timezoneRequirementRaw: "UTC+3"),
            TestFactory.Profile(skills: [new SkillCriterion(".net", 10, 5), new SkillCriterion("c#", 10, 5)]));
        Assert.InRange(score.Final, 0, 10);
    }
}
