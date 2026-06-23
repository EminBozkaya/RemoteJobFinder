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

    // Faz 5a: arayüzden düzenlenebilir alanlar (set). Değişince RecomputeService cache'ten yeniden hesaplar.
    public string ResidenceCountry { get; set; } = "TR";

    public IReadOnlyList<string> RequiredKeywords { get; set; } = [];
    public IReadOnlyList<string> ForbiddenKeywords { get; set; } = [];
    public IReadOnlyList<string> NiceKeywords { get; set; } = [];

    // 5b'ye ertelendi (henüz decider/scoring kullanmıyor): ContractTypes, Salary.
    public IReadOnlyList<string> ContractTypes { get; init; } = [];

    public int TimezoneToleranceHours { get; set; } = 4;
    public decimal? SalaryMin { get; init; }
    public string? SalaryCurrency { get; init; }
    public double MinScoreToShow { get; set; } = 5.0;

    public bool IsActive { get; init; } = true;
}
