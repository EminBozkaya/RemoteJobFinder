using JobScanner.Domain.Users;

namespace JobScanner.Application.Abstractions;

/// <summary>Faz 5a: arayüzden düzenlenebilir kriter alanları (sadece sonucu etkileyenler).</summary>
public sealed record ProfileEdit(
    string ResidenceCountry,
    IReadOnlyList<string> RequiredKeywords,
    IReadOnlyList<string> ForbiddenKeywords,
    IReadOnlyList<string> NiceKeywords,
    int TimezoneToleranceHours,
    double MinScoreToShow);

/// <summary>Aktif kriter profillerini okur (Faz 1: tek seed profil) ve günceller (Faz 5a).</summary>
public interface IProfileRepository
{
    Task<IReadOnlyList<CriteriaProfile>> GetActiveAsync(CancellationToken ct);

    /// <summary>Düzenlenebilir alanları günceller. true: bulundu+kaydedildi; false: profil yok.</summary>
    Task<bool> UpdateAsync(long profileId, ProfileEdit edit, CancellationToken ct);
}
