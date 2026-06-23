using JobScanner.Domain.Users;

namespace JobScanner.Application.Abstractions;

/// <summary>Arayüzden düzenlenebilir kriter alanları (5a + 5b: yetkinlik/dil/soft skill).</summary>
public sealed record ProfileEdit(
    string ResidenceCountry,
    IReadOnlyList<string> ForbiddenKeywords,
    IReadOnlyList<SkillCriterion> Skills,
    IReadOnlyList<LanguageCriterion> Languages,
    IReadOnlyList<string> SoftSkills,
    int TimezoneToleranceHours,
    double MinScoreToShow);

/// <summary>Aktif kriter profillerini okur (Faz 1: tek seed profil) ve günceller (Faz 5a).</summary>
public interface IProfileRepository
{
    Task<IReadOnlyList<CriteriaProfile>> GetActiveAsync(CancellationToken ct);

    /// <summary>Düzenlenebilir alanları günceller. true: bulundu+kaydedildi; false: profil yok.</summary>
    Task<bool> UpdateAsync(long profileId, ProfileEdit edit, CancellationToken ct);
}
