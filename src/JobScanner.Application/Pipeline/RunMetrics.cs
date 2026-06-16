namespace JobScanner.Application.Pipeline;

/// <summary>Her run sonunda loglanan metrik satırı.</summary>
public sealed record RunMetrics(
    int Fetched,
    int NewOrChanged,
    int Unchanged,
    int Eliminated,
    int Extracted,
    int Matches,
    int SourceErrors,
    int ExtractionErrors = 0,
    int ArchivedJobs = 0,
    int ExpiredMatches = 0);
