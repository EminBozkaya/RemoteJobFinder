using System.Xml.Linq;
using JobScanner.Application.Abstractions;
using JobScanner.Domain.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobScanner.Infrastructure.Sources.WeWorkRemotely;

/// <summary>
/// We Work Remotely RSS adaptoru. Standart RSS 2.0 + media:/dc: namespace'leri.
/// Title format: "Company: Position" — ilk ":" uzerinden bolunur (yoksa hepsi baslik kabul).
/// Etiket filtresi client-side.
/// </summary>
public sealed class WeWorkRemotelyJobSource : IJobSource
{
    public string Name => "WeWorkRemotely";

    private static readonly XNamespace MediaNs = "http://search.yahoo.com/mrss";
    private static readonly XNamespace DcNs = "http://purl.org/dc/elements/1.1/";

    private readonly HttpClient _http;
    private readonly WeWorkRemotelyOptions _options;
    private readonly ILogger<WeWorkRemotelyJobSource> _log;

    public WeWorkRemotelyJobSource(HttpClient http, IOptions<WeWorkRemotelyOptions> options, ILogger<WeWorkRemotelyJobSource> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public async Task<IReadOnlyList<RawJob>> FetchAsync(SourceQuery query, CancellationToken ct)
    {
        _log.LogDebug("WWR fetch URL: {Url}", _options.FeedUrl);

        using var resp = await _http.GetAsync(_options.FeedUrl, ct);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var doc = await XDocument.LoadAsync(stream, LoadOptions.None, ct);

        var items = doc.Root?.Element("channel")?.Elements("item") ?? [];
        var result = new List<RawJob>();

        foreach (var item in items)
        {
            if (ct.IsCancellationRequested) break;
            var raw = TryParse(item);
            if (raw is null) continue;
            if (!MatchesTags(raw, query.Tags)) continue;
            result.Add(raw);
            if (result.Count >= _options.MaxResults) break;
        }

        return result;
    }

    internal RawJob? TryParse(XElement item)
    {
        var guid = (string?)item.Element("guid");
        var link = (string?)item.Element("link");
        var externalId = ExtractExternalId(guid, link);
        if (string.IsNullOrWhiteSpace(externalId)) return null;

        var rawTitle = (string?)item.Element("title") ?? string.Empty;
        var (company, title) = SplitCompanyAndTitle(rawTitle);
        if (string.IsNullOrWhiteSpace(title)) return null;

        var description = (string?)item.Element("description") ?? string.Empty;
        var pubDate = (string?)item.Element("pubDate") ?? (string?)item.Element(DcNs + "date");

        var region = (string?)item.Element("region");
        var category = (string?)item.Element("category");
        var logo = item.Element(MediaNs + "content")?.Attribute("url")?.Value;

        var extra = new Dictionary<string, object?>
        {
            ["region"] = region,
            ["category"] = category,
            ["logoUrl"] = logo,
            ["guid"] = guid,
        };

        return new RawJob(
            SourceName: Name,
            ExternalId: externalId,
            Title: title,
            Company: company,
            DescriptionHtml: description,
            Url: link ?? string.Empty,
            ApplyUrl: null,
            PostedAtRaw: pubDate,
            Extra: extra);
    }

    /// <summary>"Company: Position" formatini parcalar; iki nokta yoksa hepsini baslik kabul eder.</summary>
    internal static (string Company, string Title) SplitCompanyAndTitle(string rawTitle)
    {
        if (string.IsNullOrWhiteSpace(rawTitle)) return (string.Empty, string.Empty);
        var idx = rawTitle.IndexOf(':');
        if (idx < 0) return (string.Empty, rawTitle.Trim());

        // Iki nokta sonda → splitlemenin anlami yok, hepsini baslik kabul et
        if (idx >= rawTitle.Length - 1) return (string.Empty, rawTitle.Trim());

        var company = idx == 0 ? string.Empty : rawTitle[..idx].Trim();
        var title = rawTitle[(idx + 1)..].Trim();
        return (company, title);
    }

    /// <summary>guid yoksa link'in son segmentinden ID turet.</summary>
    internal static string? ExtractExternalId(string? guid, string? link)
    {
        if (!string.IsNullOrWhiteSpace(guid)) return guid;
        if (string.IsNullOrWhiteSpace(link)) return null;
        if (Uri.TryCreate(link, UriKind.Absolute, out var uri))
        {
            var seg = uri.Segments.LastOrDefault()?.Trim('/');
            if (!string.IsNullOrWhiteSpace(seg)) return seg;
        }
        return link; // son care: link'in kendisi
    }

    private static bool MatchesTags(RawJob job, IReadOnlyList<string> wanted)
    {
        if (wanted.Count == 0) return true;
        var haystack = $"{job.Title}\n{job.DescriptionHtml}";
        return wanted.Any(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase));
    }
}
