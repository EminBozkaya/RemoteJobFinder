using JobScanner.Domain.Eligibility;
using JobScanner.Domain.Jobs;

namespace JobScanner.Application.Abstractions;

/// <summary>
/// LLM tabanlı fact extractor: ilandan yapılandırılmış GERÇEKLER çıkarır. KARAR VERMEZ.
/// Faz 1'de stub (LLM yok); gerçek implementasyon Faz 2.
/// </summary>
public interface IEligibilityExtractor
{
    Task<EligibilityFacts> ExtractAsync(JobPosting job, CancellationToken ct);
}
