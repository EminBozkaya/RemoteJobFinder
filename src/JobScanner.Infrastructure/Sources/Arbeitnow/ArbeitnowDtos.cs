using System.Text.Json.Serialization;

namespace JobScanner.Infrastructure.Sources.Arbeitnow;

internal sealed record ArbeitnowResponse(
    [property: JsonPropertyName("data")] IReadOnlyList<ArbeitnowJob>? Data);

internal sealed record ArbeitnowJob(
    [property: JsonPropertyName("slug")] string? Slug,
    [property: JsonPropertyName("company_name")] string? CompanyName,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("remote")] bool? Remote,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("tags")] IReadOnlyList<string>? Tags,
    [property: JsonPropertyName("job_types")] IReadOnlyList<string>? JobTypes,
    [property: JsonPropertyName("location")] string? Location,
    [property: JsonPropertyName("created_at")] long? CreatedAt,
    [property: JsonPropertyName("visa_sponsorship")] bool? VisaSponsorship);
