using JobScanner.Domain.Eligibility;

namespace JobScanner.Application.Abstractions;

/// <summary>
/// HAM gerçekleri cache'ler. Anahtar: JobId + PromptVersion + ModelVersion + VersionHash.
/// Kararı/verdict'i ASLA cache'lemez.
/// </summary>
public interface IFactsCache
{
    Task<EligibilityFacts?> GetAsync(long jobId, string promptVersion, string modelVersion, string versionHash, CancellationToken ct);
    Task SetAsync(EligibilityFacts facts, CancellationToken ct);
}
