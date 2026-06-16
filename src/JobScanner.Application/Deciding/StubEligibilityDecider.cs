using JobScanner.Application.Abstractions;
using JobScanner.Domain.Eligibility;
using JobScanner.Domain.Enums;
using JobScanner.Domain.Users;

namespace JobScanner.Application.Deciding;

/// <summary>
/// Faz 1 STUB: gerçek uygunluk kararı (sert eleyiciler + EOR düzeltmesi) Faz 2'de.
/// LLM gerçeği henüz çıkarılmadığı için tüm geçenleri 'Uncertain' işaretler.
/// </summary>
public sealed class StubEligibilityDecider : IEligibilityDecider
{
    private static readonly string[] StubReason = ["Faz 1 stub: karar motoru henüz devrede değil (Faz 2)"];

    public (Decision Decision, IReadOnlyList<string> Reasons) Decide(EligibilityFacts facts, CriteriaProfile profile)
        => (Decision.Uncertain, StubReason);
}
