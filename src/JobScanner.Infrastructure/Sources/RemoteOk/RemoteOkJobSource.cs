using System.Text.Json;
using System.Text.Json.Serialization;
using JobScanner.Application.Abstractions;
using JobScanner.Domain.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobScanner.Infrastructure.Sources.RemoteOk;

/// <summary>
/// RemoteOK API adaptoru. Tek GET ile tum ilanlari array olarak doner;
/// ilk eleman metadata (legal/last_updated) — atlanir.
/// Etiket filtresi client-side (RemoteOK API'sinin sunucu tarafi filtresi yok).
/// </summary>
public sealed class RemoteOkJobSource : IJobSource
{
    public string Name => "RemoteOK";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private readonly HttpClient _http;
    private readonly RemoteOkOptions _options;
    private readonly ILogger<RemoteOkJobSource> _log;

    public RemoteOkJobSource(HttpClient http, IOptions<RemoteOkOptions> options, ILogger<RemoteOkJobSource> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public async Task<IReadOnlyList<RawJob>> FetchAsync(SourceQuery query, CancellationToken ct)
    {
        _log.LogDebug("RemoteOK fetch URL: {Url}", _options.BaseUrl);

        using var resp = await _http.GetAsync(_options.BaseUrl, ct);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var payload = await JsonSerializer.DeserializeAsync<List<RemoteOkJob>>(stream, JsonOpts, ct);
        if (payload is null) return [];

        var jobs = payload
            .Where(j => !string.IsNullOrEmpty(j.Id) && j.Legal is null)
            .Where(j => MatchesTags(j, query.Tags))
            .Take(_options.MaxResults)
            .Select(MapToRaw)
            .ToList();

        return jobs;
    }

    /// <summary>Etiket filtresi: bos sorgu → hepsi gec. Aksi halde ilanin tag listesi
    /// veya basligi sorulan etiketlerden en az birini icermeli (OR mantigi).</summary>
    private static bool MatchesTags(RemoteOkJob j, IReadOnlyList<string> wanted)
    {
        if (wanted.Count == 0) return true;
        var haystack = string.Join(' ', (j.Tags ?? []).Append(j.Position ?? string.Empty));
        return wanted.Any(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    private RawJob MapToRaw(RemoteOkJob j)
    {
        var extra = new Dictionary<string, object?>
        {
            ["tags"] = j.Tags,
            ["location"] = j.Location,
            ["salaryMin"] = j.SalaryMin,
            ["salaryMax"] = j.SalaryMax,
            ["companyLogo"] = j.CompanyLogo,
            ["slug"] = j.Slug,
        };

        // RemoteOK 'url' yoksa slug'tan turet.
        var url = !string.IsNullOrWhiteSpace(j.Url)
            ? j.Url!
            : !string.IsNullOrWhiteSpace(j.Slug)
                ? $"https://remoteok.com/remote-jobs/{j.Slug}"
                : $"https://remoteok.com/remote-jobs/{j.Id}";

        return new RawJob(
            SourceName: Name,
            ExternalId: j.Id!,
            Title: j.Position ?? string.Empty,
            Company: j.Company ?? string.Empty,
            DescriptionHtml: j.Description ?? string.Empty,
            Url: url,
            ApplyUrl: string.IsNullOrWhiteSpace(j.ApplyUrl) ? null : j.ApplyUrl,
            PostedAtRaw: j.Date,
            Extra: extra);
    }
}
