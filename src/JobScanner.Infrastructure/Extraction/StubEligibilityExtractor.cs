using JobScanner.Application.Abstractions;
using JobScanner.Domain.Eligibility;
using JobScanner.Domain.Enums;
using JobScanner.Domain.Jobs;

namespace JobScanner.Infrastructure.Extraction;

/// <summary>
/// LLM kapalıyken (Llm:Enabled=false) kullanılır: boş/Unknown gerçekler (Confidence 0).
/// Decider bunları düşük güven nedeniyle Uncertain işaretler.
/// </summary>
public sealed class StubEligibilityExtractor : IEligibilityExtractor
{
    private readonly IExtractionVersion _version;
    private readonly TimeProvider _clock;

    public StubEligibilityExtractor(IExtractionVersion version, TimeProvider clock)
    {
        _version = version;
        _clock = clock;
    }

    public Task<EligibilityFacts> ExtractAsync(JobPosting job, CancellationToken ct)
    {
        var facts = new EligibilityFacts(
            JobId: job.Id,
            PromptVersion: _version.PromptVersion,
            ModelVersion: _version.ModelVersion,
            VersionHash: job.VersionHash,
            RequiresWorkAuth: null,
            RequiresRelocation: null,
            BackgroundCheckCountry: null,
            AllowedCountries: null,
            RequiresCitizenship: null,
            AllowsB2BContractor: null,
            EngagementType: EngagementType.Unknown,
            MentionsEor: null,
            EorPlatform: null,
            DataBoundary: null,
            TimezoneRequirementRaw: null,
            IsRecruiterAgency: null,
            IsLikelyGhost: null,
            Confidence: 0,
            ExtractedAt: _clock.GetUtcNow(),
            RawJson: "{}");

        return Task.FromResult(facts);
    }
}
