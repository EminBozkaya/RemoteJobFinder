using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobScanner.Application.Abstractions;
using JobScanner.Domain.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobScanner.Infrastructure.Sources.Arbeitnow;

/// <summary>
/// Arbeitnow job-board-api adaptoru. Sunucu-tarafi filtre yok; tum sayfa cekilir,
/// client-side remote + tag filtreleme uygulanir.
/// </summary>
public sealed class ArbeitnowJobSource : IJobSource
{
    public string Name => "Arbeitnow";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private readonly HttpClient _http;
    private readonly ArbeitnowOptions _options;
    private readonly ILogger<ArbeitnowJobSource> _log;

    public ArbeitnowJobSource(HttpClient http, IOptions<ArbeitnowOptions> options, ILogger<ArbeitnowJobSource> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public async Task<IReadOnlyList<RawJob>> FetchAsync(SourceQuery query, CancellationToken ct)
    {
        _log.LogDebug("Arbeitnow fetch URL: {Url}", _options.BaseUrl);

        using var resp = await _http.GetAsync(_options.BaseUrl, ct);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var payload = await JsonSerializer.DeserializeAsync<ArbeitnowResponse>(stream, JsonOpts, ct);
        if (payload?.Data is null) return [];

        var result = new List<RawJob>();
        foreach (var j in payload.Data)
        {
            if (string.IsNullOrEmpty(j.Slug) || string.IsNullOrWhiteSpace(j.Url)) continue;
            if (_options.RemoteOnly && j.Remote != true) continue;
            if (!MatchesTags(j, query.Tags)) continue;

            result.Add(MapToRaw(j));
            if (result.Count >= _options.MaxResults) break;
        }
        return result;
    }

    /// <summary>Etiket filtresi: bos sorgu → hepsi gec; aksi halde title+tags+description'da OR ara.</summary>
    private static bool MatchesTags(ArbeitnowJob j, IReadOnlyList<string> wanted)
    {
        if (wanted.Count == 0) return true;
        var haystack = string.Join(' ',
            j.Title ?? string.Empty,
            string.Join(' ', j.Tags ?? []),
            j.Description ?? string.Empty);
        return wanted.Any(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    private RawJob MapToRaw(ArbeitnowJob j)
    {
        var extra = new Dictionary<string, object?>
        {
            ["tags"] = j.Tags,
            ["jobTypes"] = j.JobTypes,
            ["location"] = j.Location,
            ["visaSponsorship"] = j.VisaSponsorship, // EU şirketlerinin visa açıklığı — değerli sinyal
            ["remote"] = j.Remote,
        };

        // created_at unix timestamp ise ISO'ya çevir; karişik olabilir
        var postedAt = j.CreatedAt.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(j.CreatedAt.Value).ToString("O", CultureInfo.InvariantCulture)
            : null;

        return new RawJob(
            SourceName: Name,
            ExternalId: j.Slug!,
            Title: j.Title ?? string.Empty,
            Company: j.CompanyName ?? string.Empty,
            DescriptionHtml: j.Description ?? string.Empty,
            Url: j.Url!,
            ApplyUrl: null,
            PostedAtRaw: postedAt,
            Extra: extra);
    }
}
