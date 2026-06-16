using JobScanner.Domain.Users;

namespace JobScanner.Application.Abstractions;

/// <summary>Aktif kriter profillerini okur (Faz 1: tek seed profil).</summary>
public interface IProfileRepository
{
    Task<IReadOnlyList<CriteriaProfile>> GetActiveAsync(CancellationToken ct);
}
