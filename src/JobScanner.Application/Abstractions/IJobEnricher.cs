using JobScanner.Domain.Jobs;

namespace JobScanner.Application.Abstractions;

/// <summary>REZERVE: ilan zenginleştirme (ileride). Faz 1'de no-op implementasyon.</summary>
public interface IJobEnricher
{
    Task EnrichAsync(JobPosting job, CancellationToken ct);
}
