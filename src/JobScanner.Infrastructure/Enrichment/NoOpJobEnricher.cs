using JobScanner.Application.Abstractions;
using JobScanner.Domain.Jobs;

namespace JobScanner.Infrastructure.Enrichment;

/// <summary>REZERVE: ilan zenginlestirme henuz yok. No-op (ileride gercek implementasyon).</summary>
public sealed class NoOpJobEnricher : IJobEnricher
{
    public Task EnrichAsync(JobPosting job, CancellationToken ct) => Task.CompletedTask;
}
