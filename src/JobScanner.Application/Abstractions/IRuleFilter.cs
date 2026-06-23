using JobScanner.Domain.Jobs;
using JobScanner.Domain.Users;

namespace JobScanner.Application.Abstractions;

/// <summary>
/// Ucuz, tipli, yan etkisiz kural elemesi: WorkMode != Remote veya profilin yasaklı
/// keyword'lerinden biri geçerse eler. Forbidden keyword'ler artık profilden (Faz 5a) gelir.
/// </summary>
public interface IRuleFilter
{
    RuleResult Evaluate(JobPosting job, CriteriaProfile profile);
}
