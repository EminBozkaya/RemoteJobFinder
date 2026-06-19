using JobScanner.Domain.Applications;
using JobScanner.Domain.Jobs;
using JobScanner.Domain.Users;

namespace JobScanner.Application.Abstractions;

/// <summary>
/// İlana özel başvuru materyali (cover letter + uyarlanmış CV) üretir. LLM tabanlı; implementasyon
/// Infrastructure'da (IChatClient, sağlayıcı-bağımsız). Faz 4: LLM rolü "gerçek çıkarımı"ndan
/// "materyal üretimi"ne genişler — KARAR/puan hâlâ saf C#'tadır.
/// </summary>
public interface IApplicationMaterialGenerator
{
    /// <summary>LLM yapılandırılmış ve kullanılabilir mi? false ise üretim yapılamaz.</summary>
    bool Available { get; }

    /// <summary>Materyal prompt sürümü (tazeleme anahtarının parçası).</summary>
    string PromptVersion { get; }

    /// <summary>Sağlayıcı + model (model değişince saklı materyal bayatlar).</summary>
    string ModelVersion { get; }

    /// <summary>İlan + profil + ana CV'den materyal üretir. Dil ilan diline uydurulur.</summary>
    Task<GeneratedMaterials> GenerateAsync(
        JobPosting job, CriteriaProfile profile, string baseCvMarkdown, CancellationToken ct);
}
