using JobScanner.Domain.Enums;

namespace JobScanner.Domain.Matching;

/// <summary>
/// Bir profil ile bir ilanın eşleşmesi (ProfileId, JobId). Karar/puan saf C#'ta
/// EligibilityFacts'ten hesaplanır.
/// </summary>
public sealed class UserJobMatch
{
    public required long ProfileId { get; init; }
    public required long JobId { get; init; }
    public double Score { get; set; }
    public string ScoreBreakdownJson { get; set; } = "[]";   // [{criterion, contribution}]
    public Decision Decision { get; set; }                   // C#'ta facts'ten hesaplanır
    public string DecisionReasonsJson { get; set; } = "[]";
    public MatchState State { get; set; } = MatchState.New;
    public string? Feedback { get; set; }                    // GoodFit | BadFit | null
    public DateTimeOffset? OpenedAt { get; set; }
    public DateTimeOffset? AppliedAt { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
