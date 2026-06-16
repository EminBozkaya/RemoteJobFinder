using JobScanner.Domain.Jobs;

namespace JobScanner.Application.Abstractions;

/// <summary>
/// Ucuz, tipli, yan etkisiz kural elemesi (WorkMode + global forbidden/required keyword'ler).
/// Profil-bağımsızdır; profil bazlı karar/puan sonraki aşamada (Decider/Scoring) yapılır.
/// </summary>
public interface IRuleFilter
{
    RuleResult Evaluate(JobPosting job);
}
