using System.Globalization;
using System.Text.RegularExpressions;

namespace JobScanner.Application.Scoring;

/// <summary>
/// LLM'in çıkardığı serbest-metin timezone gereksinimini (ör. "EST 9-5", "UTC+1 core hours")
/// UTC'ye göre saat ofsetine çevirir. Çözülemezse null. Saf + yan etkisiz.
/// </summary>
internal static partial class TimezoneParser
{
    // Yaygın timezone kısaltmaları → UTC ofseti (saat).
    private static readonly Dictionary<string, double> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["UTC"] = 0, ["GMT"] = 0, ["WET"] = 0, ["BST"] = 1, ["IST_IE"] = 1,
        ["CET"] = 1, ["CEST"] = 2, ["EET"] = 2, ["EEST"] = 3, ["MSK"] = 3, ["TRT"] = 3,
        ["EST"] = -5, ["EDT"] = -4, ["CST"] = -6, ["CDT"] = -5, ["MST"] = -7, ["MDT"] = -6,
        ["PST"] = -8, ["PDT"] = -7, ["AKST"] = -9, ["HST"] = -10,
        ["IST"] = 5.5, ["JST"] = 9, ["SGT"] = 8, ["HKT"] = 8, ["AEST"] = 10, ["AEDT"] = 11,
    };

    public static double? TryParseUtcOffset(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        // UTC+1 / GMT-5 / UTC+5:30
        var m = OffsetRegex().Match(raw);
        if (m.Success)
        {
            var sign = m.Groups[2].Value == "-" ? -1 : 1;
            var hours = int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
            var minutes = m.Groups[4].Success ? int.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture) : 0;
            return sign * (hours + minutes / 60.0);
        }

        // Bilinen kısaltmalar (kelime sınırı ile)
        foreach (var token in WordRegex().Matches(raw).Select(x => x.Value))
        {
            if (Abbreviations.TryGetValue(token, out var offset))
                return offset;
        }

        return null;
    }

    [GeneratedRegex(@"(UTC|GMT)\s*([+-])\s*(\d{1,2})(?::?(\d{2}))?", RegexOptions.IgnoreCase)]
    private static partial Regex OffsetRegex();

    [GeneratedRegex(@"[A-Za-z]{2,5}")]
    private static partial Regex WordRegex();
}
