using JobScanner.Domain.Eligibility;
using JobScanner.Domain.Enums;
using JobScanner.Domain.Users;

namespace JobScanner.Application.Abstractions;

/// <summary>
/// Saf C# uygunluk kararı: gerçeklerden + profilden Eligible/Ineligible/Uncertain üretir.
/// Token harcamaz; kriter değişince ucuzca yeniden hesaplanır.
/// </summary>
public interface IEligibilityDecider
{
    (Decision Decision, IReadOnlyList<string> Reasons) Decide(EligibilityFacts facts, CriteriaProfile profile);
}
