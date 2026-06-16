namespace JobScanner.Domain.Jobs;

/// <summary>
/// Bir kaynaktan (IJobSource) ham olarak çekilen, henüz normalize edilmemiş ilan.
/// </summary>
public sealed record RawJob(
    string SourceName,
    string ExternalId,
    string Title,
    string Company,
    string DescriptionHtml,
    string Url,
    string? ApplyUrl,
    string? PostedAtRaw,
    IReadOnlyDictionary<string, object?> Extra);
