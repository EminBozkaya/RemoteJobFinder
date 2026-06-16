using JobScanner.Domain.Jobs;

namespace JobScanner.Application.Abstractions;

/// <summary>Bir remote iş ilanı API'sinden ham ilan çeken adaptör. Web scraping yok.</summary>
public interface IJobSource
{
    string Name { get; }
    Task<IReadOnlyList<RawJob>> FetchAsync(SourceQuery query, CancellationToken ct);
}
