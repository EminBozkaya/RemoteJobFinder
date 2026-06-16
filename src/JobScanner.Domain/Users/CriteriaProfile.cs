using JobScanner.Domain.Enums;

namespace JobScanner.Domain.Users;

/// <summary>
/// Bir kullanıcının arama/eleme kriterleri. Başlangıçta güçlü-tipli alanlar
/// (dinamik Target/Operator UI yok). RuleFilter, Decider ve Scoring bunu okur.
/// </summary>
public sealed class CriteriaProfile
{
    public long Id { get; init; }
    public required long UserId { get; init; }
    public required string Name { get; init; }

    public WorkMode WorkMode { get; init; } = WorkMode.Remote;
    public string ResidenceCountry { get; init; } = "TR";

    public IReadOnlyList<string> RequiredKeywords { get; init; } = [];
    public IReadOnlyList<string> ForbiddenKeywords { get; init; } = [];
    public IReadOnlyList<string> NiceKeywords { get; init; } = [];
    public IReadOnlyList<string> ContractTypes { get; init; } = [];

    public int TimezoneToleranceHours { get; init; } = 4;
    public decimal? SalaryMin { get; init; }
    public string? SalaryCurrency { get; init; }
    public double MinScoreToShow { get; init; } = 5.0;

    public bool IsActive { get; init; } = true;
}
