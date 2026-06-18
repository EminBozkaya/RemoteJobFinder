using System.Text.Json.Serialization;

namespace JobScanner.Infrastructure.Sources.Remotive;

/// <summary>Remotive yanit zarfi. "00-warning" / "0-legal-notice" alanlari yok sayilir (toleransli).</summary>
internal sealed record RemotiveResponse(
    [property: JsonPropertyName("job-count")] int? JobCount,
    [property: JsonPropertyName("jobs")] IReadOnlyList<RemotiveJob>? Jobs);

internal sealed record RemotiveJob(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("company_name")] string? CompanyName,
    [property: JsonPropertyName("company_logo")] string? CompanyLogo,
    [property: JsonPropertyName("category")] string? Category,
    [property: JsonPropertyName("tags")] IReadOnlyList<string>? Tags,
    [property: JsonPropertyName("job_type")] string? JobType,
    [property: JsonPropertyName("publication_date")] string? PublicationDate,
    [property: JsonPropertyName("candidate_required_location")] string? CandidateRequiredLocation,
    [property: JsonPropertyName("salary")] string? Salary,
    [property: JsonPropertyName("description")] string? Description);
