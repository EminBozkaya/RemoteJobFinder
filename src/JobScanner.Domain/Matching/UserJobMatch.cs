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
    public LegitimacyConfidence Legitimacy { get; set; } = LegitimacyConfidence.High;
    public string LegitimacySignalsJson { get; set; } = "[]"; // sinyal etiketleri (ghost/recruiter/old/low-confidence)
    public MatchState State { get; set; } = MatchState.New;
    public string? Feedback { get; set; }                    // GoodFit | BadFit | null
    public DateTimeOffset? OpenedAt { get; set; }
    public DateTimeOffset? AppliedAt { get; set; }
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Kullanıcı kararını verdi (Applied/Dismissed) → yeniden işlenmez.</summary>
    public bool IsClosed => State is MatchState.Applied or MatchState.Dismissed;

    // --- Durum makinesi (saf geçişler). Etkileşimli tetikleyiciler Faz 3'te (API butonları). ---

    public void Save()
    {
        if (State == MatchState.New) State = MatchState.Saved;
    }

    public void Open(DateTimeOffset now)
    {
        if (IsClosed) return;
        State = MatchState.Opened;
        OpenedAt ??= now;
    }

    public void Apply(DateTimeOffset now)
    {
        State = MatchState.Applied;
        AppliedAt ??= now;
    }

    public void Dismiss() => State = MatchState.Dismissed;

    /// <summary>İlan arşivlendiğinde açık eşleşme süresi dolar (kullanıcı kararı korunur).</summary>
    public void Expire()
    {
        if (!IsClosed && State != MatchState.Expired) State = MatchState.Expired;
    }
}
