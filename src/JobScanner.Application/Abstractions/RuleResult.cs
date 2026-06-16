namespace JobScanner.Application.Abstractions;

public enum FilterDecision { Eliminate, Pass, NeedsExtraction }

/// <summary>
/// Ucuz, tipli kural elemesi sonucu. Eliminate → hemen ele; Pass → uygun;
/// NeedsExtraction → belirsiz, LLM fact extraction gerekir (Faz 2).
/// </summary>
public sealed record RuleResult(FilterDecision Decision, IReadOnlyList<string> Reasons)
{
    public static RuleResult Eliminate(params string[] reasons) => new(FilterDecision.Eliminate, reasons);
    public static RuleResult Pass(params string[] reasons) => new(FilterDecision.Pass, reasons);
    public static RuleResult NeedsExtraction(params string[] reasons) => new(FilterDecision.NeedsExtraction, reasons);
}
