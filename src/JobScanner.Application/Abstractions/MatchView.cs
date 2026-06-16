namespace JobScanner.Application.Abstractions;

/// <summary>Okuma tarafı: bir eşleşmenin ilan bilgisiyle birleştirilmiş görünümü (read-only API için).</summary>
public sealed record MatchView(
    long ProfileId,
    long JobId,
    string Title,
    string Company,
    string Url,
    string? ApplyUrl,
    double Score,
    string Decision,
    string State,
    DateTimeOffset? PostedAt,
    string ScoreBreakdownJson,
    string DecisionReasonsJson);
