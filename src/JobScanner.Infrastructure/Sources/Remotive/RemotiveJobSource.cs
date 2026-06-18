using System.Text.Json;
using System.Text.Json.Serialization;
using JobScanner.Application.Abstractions;
using JobScanner.Domain.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobScanner.Infrastructure.Sources.Remotive;

/// <summary>
/// Remotive remote-jobs API adaptoru. ToS: Jobicy'e benzer — 'search' tek anahtar kelime,
/// cok etiket icin etiket basina ayri istek + merge.
/// </summary>
public sealed class RemotiveJobSource : IJobSource
{
    public string Name => "Remotive";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private readonly HttpClient _http;
    private readonly RemotiveOptions _options;
    private readonly ILogger<RemotiveJobSource> _log;

    public RemotiveJobSource(HttpClient http, IOptions<RemotiveOptions> options, ILogger<RemotiveJobSource> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public async Task<IReadOnlyList<RawJob>> FetchAsync(SourceQuery query, CancellationToken ct)
    {
        var tags = query.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        var merged = new Dictionary<string, RawJob>();

        if (tags.Count == 0)
        {
            await FetchPageAsync(BuildUrl(search: null), merged, ct);
        }
        else
        {
            foreach (var tag in tags)
            {
                try
                {
                    await FetchPageAsync(BuildUrl(tag), merged, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.LogWarning(ex, "Remotive tag '{Tag}' istegi basarisiz; diger etiketlerle devam", tag);
                }
            }
        }

        return merged.Values.ToList();
    }

    private async Task FetchPageAsync(string url, Dictionary<string, RawJob> sink, CancellationToken ct)
    {
        _log.LogDebug("Remotive fetch URL: {Url}", url);
        using var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var payload = await JsonSerializer.DeserializeAsync<RemotiveResponse>(stream, JsonOpts, ct);

        foreach (var j in payload?.Jobs ?? [])
        {
            if (j.Id == 0 || string.IsNullOrWhiteSpace(j.Url)) continue;
            sink[j.Id.ToString()] = MapToRaw(j);
        }
    }

    private string BuildUrl(string? search)
    {
        var limit = Math.Clamp(_options.LimitPerTag, 1, 50);
        var url = $"{_options.BaseUrl}?limit={limit}";

        if (!string.IsNullOrWhiteSpace(_options.Category))
            url += $"&category={Uri.EscapeDataString(_options.Category)}";

        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";

        return url;
    }

    private RawJob MapToRaw(RemotiveJob j)
    {
        var extra = new Dictionary<string, object?>
        {
            ["category"] = j.Category,
            ["tags"] = j.Tags,
            ["jobType"] = j.JobType,
            ["candidateRequiredLocation"] = j.CandidateRequiredLocation,
            ["salary"] = j.Salary,
            ["companyLogo"] = j.CompanyLogo,
        };

        return new RawJob(
            SourceName: Name,
            ExternalId: j.Id.ToString(),
            Title: j.Title ?? string.Empty,
            Company: j.CompanyName ?? string.Empty,
            DescriptionHtml: j.Description ?? string.Empty,
            Url: j.Url!,
            ApplyUrl: null,
            PostedAtRaw: j.PublicationDate,
            Extra: extra);
    }
}
