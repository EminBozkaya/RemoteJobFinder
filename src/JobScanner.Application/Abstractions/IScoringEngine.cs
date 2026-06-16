using JobScanner.Domain.Eligibility;
using JobScanner.Domain.Jobs;
using JobScanner.Domain.Users;

namespace JobScanner.Application.Abstractions;

/// <summary>Saf C# puanlama: ağırlıklı toplam + açıklanabilir döküm. ML yok.</summary>
public interface IScoringEngine
{
    JobScore Score(JobPosting job, EligibilityFacts facts, CriteriaProfile profile);
}
