using System.Text.Json;
using System.Text.Json.Serialization;
using JobScanner.Application.Abstractions;
using JobScanner.Domain.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobScanner.Infrastructure.Sources.Jobicy;

/// <summary>
/// Jobicy remote-jobs API adaptoru. HttpClient + toleransli System.Text.Json.
/// Web scraping yok; yalniz yapilandirilmis API.
/// </summary>
public sealed class JobicyJobSource : IJobSource
{
    public string Name => "Jobicy";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private readonly HttpClient _http;
    private readonly JobicyOptions _options;
    private readonly ILogger<JobicyJobSource> _log;

    public JobicyJobSource(HttpClient http, IOptions<JobicyOptions> options, ILogger<JobicyJobSource> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public async Task<IReadOnlyList<RawJob>> FetchAsync(SourceQuery query, CancellationToken ct)
    {
        // Jobicy 'tag' tek anahtar kelimedir; cok etiket icin etiket basina ayri istek + birlestir.
        var tags = query.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        var merged = new Dictionary<string, RawJob>();

        if (tags.Count == 0)
        {
            await FetchPageAsync(BuildUrl(query, tag: null), merged, ct);
        }
        else
        {
            foreach (var tag in tags)
            {
                try
                {
                    await FetchPageAsync(BuildUrl(query, tag), merged, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.LogWarning(ex, "Jobicy tag '{Tag}' istegi basarisiz; diger etiketlerle devam", tag);
                }
            }
        }

        return merged.Values.ToList();
    }

    private async Task FetchPageAsync(string url, Dictionary<string, RawJob> sink, CancellationToken ct)
    {
        _log.LogDebug("Jobicy fetch URL: {Url}", url);

        using var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var payload = await JsonSerializer.DeserializeAsync<JobicyResponse>(stream, JsonOpts, ct);

        foreach (var j in payload?.Jobs ?? [])
        {
            if (j.Id == 0 || string.IsNullOrWhiteSpace(j.Url)) continue; // toleransli: bozuk kaydi atla
            sink[j.Id.ToString()] = MapToRaw(j); // ayni ilan birden cok etikette gelirse tekille
        }
    }

    private string BuildUrl(SourceQuery query, string? tag)
    {
        var count = Math.Clamp(query.Count > 0 ? query.Count : _options.Count, 1, 50);
        var url = $"{_options.BaseUrl}?count={count}";

        // Jobicy 'geo' bolge slug'i bekler; bos veya 'anywhere' ise filtre uygulanmaz.
        if (!string.IsNullOrWhiteSpace(query.Geo) &&
            !query.Geo.Equals("anywhere", StringComparison.OrdinalIgnoreCase))
        {
            url += $"&geo={Uri.EscapeDataString(query.Geo)}";
        }

        if (!string.IsNullOrWhiteSpace(tag))
            url += $"&tag={Uri.EscapeDataString(tag)}";

        return url;
    }

    private RawJob MapToRaw(JobicyJob j)
    {
        var extra = new Dictionary<string, object?>
        {
            ["jobGeo"] = j.JobGeo,
            ["jobLevel"] = j.JobLevel,
            ["jobType"] = j.JobType,
            ["jobIndustry"] = j.JobIndustry,
            ["annualSalaryMin"] = j.AnnualSalaryMin,
            ["annualSalaryMax"] = j.AnnualSalaryMax,
            ["salaryCurrency"] = j.SalaryCurrency,
        };

        var description = !string.IsNullOrWhiteSpace(j.JobDescription) ? j.JobDescription : j.JobExcerpt ?? string.Empty;

        return new RawJob(
            SourceName: Name,
            ExternalId: j.Id.ToString(),
            Title: j.JobTitle ?? string.Empty,
            Company: j.CompanyName ?? string.Empty,
            DescriptionHtml: description,
            Url: j.Url!,
            ApplyUrl: null,
            PostedAtRaw: j.PubDate,
            Extra: extra);
    }
}
