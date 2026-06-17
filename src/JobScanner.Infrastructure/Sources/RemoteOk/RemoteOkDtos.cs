using System.Text.Json.Serialization;

namespace JobScanner.Infrastructure.Sources.RemoteOk;

/// <summary>
/// RemoteOK tek ilan kaydi. Dizinin ilk elemani metadata olabilir (legal/last_updated);
/// bu DTO o durumda bos kalir, isaretlenmis Id ile filtrelenir.
/// </summary>
internal sealed record RemoteOkJob(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("slug")] string? Slug,
    [property: JsonPropertyName("epoch")] long? Epoch,
    [property: JsonPropertyName("date")] string? Date,
    [property: JsonPropertyName("company")] string? Company,
    [property: JsonPropertyName("company_logo")] string? CompanyLogo,
    [property: JsonPropertyName("position")] string? Position,
    [property: JsonPropertyName("tags")] IReadOnlyList<string>? Tags,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("location")] string? Location,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("apply_url")] string? ApplyUrl,
    [property: JsonPropertyName("salary_min")] double? SalaryMin,
    [property: JsonPropertyName("salary_max")] double? SalaryMax,
    // Metadata zarfindan gelen alan; ilan kayidlarinda olmaz.
    [property: JsonPropertyName("legal")] string? Legal);
