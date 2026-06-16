using JobScanner.Application.Abstractions;
using JobScanner.Application.Pipeline;
using JobScanner.Domain.Eligibility;
using JobScanner.Domain.Enums;
using JobScanner.Domain.Jobs;
using Microsoft.Extensions.Options;

namespace JobScanner.Infrastructure.Extraction;

/// <summary>
/// Faz 1 STUB: LLM yok. Bos/Unknown gercekler dondurur (Confidence 0). Gercek LLM tabanli
/// IEligibilityExtractor (IChatClient) Faz 2'de gelir.
/// </summary>
public sealed class StubEligibilityExtractor : IEligibilityExtractor
{
    private readonly PipelineOptions _options;
    private readonly TimeProvider _clock;

    public StubEligibilityExtractor(IOptions<PipelineOptions> options, TimeProvider clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public Task<EligibilityFacts> ExtractAsync(JobPosting job, CancellationToken ct)
    {
        var facts = new EligibilityFacts(
            JobId: job.Id,
            PromptVersion: _options.PromptVersion,
            ModelVersion: _options.ModelVersion,
            VersionHash: job.VersionHash,
            RequiresWorkAuth: null,
            AllowedCountries: null,
            RequiresCitizenship: null,
            AllowsB2BContractor: null,
            EngagementType: EngagementType.Unknown,
            MentionsEor: null,
            EorPlatform: null,
            DataBoundary: null,
            TimezoneRequirementRaw: null,
            IsRecruiterAgency: null,
            Confidence: 0,
            ExtractedAt: _clock.GetUtcNow(),
            RawJson: "{}");

        return Task.FromResult(facts);
    }
}
