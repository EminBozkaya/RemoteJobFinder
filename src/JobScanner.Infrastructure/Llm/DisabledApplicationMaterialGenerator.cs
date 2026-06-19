using JobScanner.Application.Abstractions;
using JobScanner.Domain.Applications;
using JobScanner.Domain.Jobs;
using JobScanner.Domain.Users;

namespace JobScanner.Infrastructure.Llm;

/// <summary>
/// LLM kapalıyken kaydedilen "üretilemez" generator. Saklı materyal okumayı engellemez
/// (MaterialService kendi akışında Available'ı kontrol eder), yalnız üretim çağrısı yapılamaz.
/// </summary>
public sealed class DisabledApplicationMaterialGenerator : IApplicationMaterialGenerator
{
    public bool Available => false;
    public string PromptVersion => "none";
    public string ModelVersion => "none";

    public Task<GeneratedMaterials> GenerateAsync(
        JobPosting job, CriteriaProfile profile, string baseCvMarkdown, CancellationToken ct) =>
        throw new InvalidOperationException("LLM kapalı: başvuru materyali üretilemez (Llm:Enabled=true gerekli).");
}
