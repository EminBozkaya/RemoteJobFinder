using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AngleSharp.Html.Parser;
using JobScanner.Application.Abstractions;
using JobScanner.Domain.Enums;
using JobScanner.Domain.Jobs;

namespace JobScanner.Infrastructure.Normalization;

/// <summary>
/// Ham ilani normalize eder: HTML to plain text (AngleSharp), IdentityKey (cross-source dedup)
/// ve VersionHash (icerik degisimi tespiti) uretir. Saf sayilir (yalniz saat enjekte edilir).
/// </summary>
public sealed class Normalizer : IJobNormalizer
{
    // Hash alanlarini ayiran gorunmez ayirici (U+001F unit separator).
    private const string FieldSeparator = "";

    private static readonly HtmlParser HtmlParser = new();
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    private readonly TimeProvider _clock;

    public Normalizer(TimeProvider clock) => _clock = clock;

    public JobPosting Normalize(RawJob raw)
    {
        var now = _clock.GetUtcNow();
        var descriptionText = HtmlToText(raw.DescriptionHtml);
        var title = Collapse(raw.Title);
        var company = Collapse(raw.Company);

        var identityKey = BuildIdentityKey(company, title);
        var versionHash = ComputeVersionHash(title, company, descriptionText, raw.Url, raw.ApplyUrl);

        return new JobPosting
        {
            SourceName = raw.SourceName,
            ExternalId = raw.ExternalId,
            IdentityKey = identityKey,
            Title = title,
            Company = company,
            DescriptionText = descriptionText,
            Url = raw.Url,
            ApplyUrl = string.IsNullOrWhiteSpace(raw.ApplyUrl) ? null : raw.ApplyUrl,
            WorkMode = WorkMode.Remote,
            PostedAt = ParsePostedAt(raw.PostedAtRaw),
            FirstSeenAt = now,
            LastSeenAt = now,
            VersionHash = versionHash,
            SourceExtraJson = SerializeExtra(raw.Extra),
            Status = JobStatus.Active,
        };
    }

    /// <summary>AngleSharp ile HTML'i duz metne cevirir, bosluklari sadelestirir.</summary>
    public static string HtmlToText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var doc = HtmlParser.ParseDocument(html);
        var text = doc.Body?.TextContent ?? doc.DocumentElement.TextContent;
        return Collapse(text);
    }

    /// <summary>trim + ic bosluklari tek bosluga indirger (buyuk/kucuk harf korunur).</summary>
    public static string Collapse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var sb = new StringBuilder(value.Length);
        var prevSpace = false;
        foreach (var ch in value.Trim())
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!prevSpace) { sb.Append(' '); prevSpace = true; }
            }
            else { sb.Append(ch); prevSpace = false; }
        }
        return sb.ToString();
    }

    /// <summary>Capraz-kaynak kimlik: lower(sirket)|lower(baslik).</summary>
    public static string BuildIdentityKey(string company, string title) =>
        $"{company.ToLowerInvariant()}|{title.ToLowerInvariant()}";

    /// <summary>Icerik degisimi icin SHA-256 hash (kimlik DEGIL).</summary>
    public static string ComputeVersionHash(string title, string company, string descriptionText, string url, string? applyUrl)
    {
        var canonical = string.Join(FieldSeparator, title, company, descriptionText, url, applyUrl ?? string.Empty);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexStringLower(bytes);
    }

    private static string SerializeExtra(IReadOnlyDictionary<string, object?> extra) =>
        extra.Count == 0 ? "{}" : JsonSerializer.Serialize(extra, JsonOpts);

    private static DateTimeOffset? ParsePostedAt(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (DateTimeOffset.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal, out var dto))
            return dto.ToUniversalTime();
        return null;
    }
}
