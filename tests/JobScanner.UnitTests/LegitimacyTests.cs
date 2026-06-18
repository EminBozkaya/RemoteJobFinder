using JobScanner.Application.Deciding;
using JobScanner.Domain.Enums;
using JobScanner.Domain.Jobs;

namespace JobScanner.UnitTests;

public sealed class LegitimacyTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);

    private static JobPosting Job(DateTimeOffset firstSeen) =>
        new()
        {
            SourceName = "Jobicy", ExternalId = "1", IdentityKey = "k",
            Title = "T", Company = "C", DescriptionText = "D", Url = "https://x", VersionHash = "h",
            FirstSeenAt = firstSeen,
        };

    [Fact]
    public void Clean_facts_and_fresh_job_are_High()
    {
        var r = Legitimacy.Evaluate(TestFactory.Facts(confidence: 0.9), Job(Now.AddDays(-5)), Now);
        Assert.Equal(LegitimacyConfidence.High, r.Confidence);
        Assert.Empty(r.Signals);
    }

    [Fact]
    public void Recruiter_alone_is_Caution()
    {
        var r = Legitimacy.Evaluate(
            TestFactory.Facts(isRecruiterAgency: true, confidence: 0.9),
            Job(Now.AddDays(-5)), Now);
        Assert.Equal(LegitimacyConfidence.Caution, r.Confidence);
        Assert.Contains("recruiter-agency", r.Signals);
    }

    [Fact]
    public void Ghost_alone_is_Caution()
    {
        var r = Legitimacy.Evaluate(
            TestFactory.Facts(isLikelyGhost: true, confidence: 0.9),
            Job(Now.AddDays(-5)), Now);
        Assert.Equal(LegitimacyConfidence.Caution, r.Confidence);
        Assert.Contains("ghost-language", r.Signals);
    }

    [Fact]
    public void Long_running_alone_is_Caution()
    {
        var r = Legitimacy.Evaluate(
            TestFactory.Facts(confidence: 0.9),
            Job(Now.AddDays(-65)), Now);
        Assert.Equal(LegitimacyConfidence.Caution, r.Confidence);
        Assert.Contains(r.Signals, s => s.StartsWith("long-running-"));
    }

    [Fact]
    public void Low_llm_confidence_is_Caution()
    {
        var r = Legitimacy.Evaluate(
            TestFactory.Facts(confidence: 0.3),
            Job(Now.AddDays(-5)), Now);
        Assert.Equal(LegitimacyConfidence.Caution, r.Confidence);
        Assert.Contains("low-llm-confidence", r.Signals);
    }

    [Fact]
    public void Two_or_more_signals_are_Suspicious()
    {
        var r = Legitimacy.Evaluate(
            TestFactory.Facts(isLikelyGhost: true, isRecruiterAgency: true, confidence: 0.9),
            Job(Now.AddDays(-5)), Now);
        Assert.Equal(LegitimacyConfidence.Suspicious, r.Confidence);
        Assert.Equal(2, r.Signals.Count);
    }

    [Fact]
    public void Ghost_plus_long_running_is_Suspicious()
    {
        var r = Legitimacy.Evaluate(
            TestFactory.Facts(isLikelyGhost: true, confidence: 0.9),
            Job(Now.AddDays(-90)), Now);
        Assert.Equal(LegitimacyConfidence.Suspicious, r.Confidence);
    }

    [Fact]
    public void Exact_threshold_60_days_triggers_long_running()
    {
        var r = Legitimacy.Evaluate(
            TestFactory.Facts(confidence: 0.9),
            Job(Now.AddDays(-Legitimacy.StaleAfterDays)), Now);
        Assert.Contains(r.Signals, s => s.StartsWith("long-running-"));
    }
}
