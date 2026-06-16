using System.Text.Json.Serialization;

namespace JobScanner.Infrastructure.Sources.Jobicy;

/// <summary>Jobicy /remote-jobs yanit zarfi. Bilinmeyen alanlar yok sayilir (toleransli).</summary>
internal sealed record JobicyResponse(
    [property: JsonPropertyName("jobCount")] int? JobCount,
    [property: JsonPropertyName("jobs")] IReadOnlyList<JobicyJob>? Jobs);

/// <summary>Tek bir Jobicy ilani. Alanlar opsiyonel; sema farklari SourceExtraJson'a tasinir.</summary>
internal sealed record JobicyJob(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("jobTitle")] string? JobTitle,
    [property: JsonPropertyName("companyName")] string? CompanyName,
    [property: JsonPropertyName("jobGeo")] string? JobGeo,
    [property: JsonPropertyName("jobLevel")] string? JobLevel,
    [property: JsonPropertyName("jobType")] IReadOnlyList<string>? JobType,
    [property: JsonPropertyName("jobIndustry")] IReadOnlyList<string>? JobIndustry,
    [property: JsonPropertyName("jobExcerpt")] string? JobExcerpt,
    [property: JsonPropertyName("jobDescription")] string? JobDescription,
    [property: JsonPropertyName("pubDate")] string? PubDate,
    [property: JsonPropertyName("annualSalaryMin")] string? AnnualSalaryMin,
    [property: JsonPropertyName("annualSalaryMax")] string? AnnualSalaryMax,
    [property: JsonPropertyName("salaryCurrency")] string? SalaryCurrency);
