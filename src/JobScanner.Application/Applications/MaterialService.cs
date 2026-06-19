using JobScanner.Application.Abstractions;
using JobScanner.Domain.Applications;

namespace JobScanner.Application.Applications;

/// <summary>Materyal üretiminin sonucu (Api uygun HTTP koduna eşler).</summary>
public enum MaterialOutcome
{
    Ready,
    JobNotFound,
    ProfileNotFound,
    CvMissing,
    LlmDisabled,
}

public sealed record MaterialResult(MaterialOutcome Outcome, ApplicationMaterial? Material);

/// <summary>
/// On-demand başvuru materyali orkestrasyonu (Faz 4). Saf akış: ilan + profil + ana CV alır,
/// saklı materyal taze ise döndürür (token harcamadan), değilse generator ile üretip kalıcılaştırır.
/// LLM çağrısı IApplicationMaterialGenerator portunun ardındadır; bu sınıf sağlayıcı bilmez.
/// </summary>
public sealed class MaterialService
{
    private readonly IJobRepository _jobs;
    private readonly IProfileRepository _profiles;
    private readonly ICvSource _cv;
    private readonly IApplicationMaterialGenerator _generator;
    private readonly IApplicationMaterialRepository _repo;
    private readonly TimeProvider _clock;

    public MaterialService(
        IJobRepository jobs,
        IProfileRepository profiles,
        ICvSource cv,
        IApplicationMaterialGenerator generator,
        IApplicationMaterialRepository repo,
        TimeProvider clock)
    {
        _jobs = jobs;
        _profiles = profiles;
        _cv = cv;
        _generator = generator;
        _repo = repo;
        _clock = clock;
    }

    /// <summary>Yalnız saklı materyali döndürür (üretim YAPMAZ). LLM kapalıyken de çalışır.</summary>
    public Task<ApplicationMaterial?> GetExistingAsync(long profileId, long jobId, CancellationToken ct) =>
        _repo.GetAsync(profileId, jobId, ct);

    /// <summary>Taze saklı materyal varsa döner; yoksa (ya da forceRegenerate ise) üretir + kaydeder.</summary>
    public async Task<MaterialResult> GetOrGenerateAsync(
        long profileId, long jobId, bool forceRegenerate, CancellationToken ct)
    {
        var job = await _jobs.FindByIdAsync(jobId, ct);
        if (job is null) return new MaterialResult(MaterialOutcome.JobNotFound, null);

        var profile = (await _profiles.GetActiveAsync(ct)).FirstOrDefault(p => p.Id == profileId);
        if (profile is null) return new MaterialResult(MaterialOutcome.ProfileNotFound, null);

        var cv = await _cv.GetAsync(ct);
        if (cv is null) return new MaterialResult(MaterialOutcome.CvMissing, null);

        var existing = await _repo.GetAsync(profileId, jobId, ct);
        if (!forceRegenerate
            && existing is not null
            && existing.IsFreshFor(cv.Hash, _generator.PromptVersion, _generator.ModelVersion, job.VersionHash))
        {
            return new MaterialResult(MaterialOutcome.Ready, existing);
        }

        if (!_generator.Available) return new MaterialResult(MaterialOutcome.LlmDisabled, null);

        var generated = await _generator.GenerateAsync(job, profile, cv.Markdown, ct);

        var material = existing ?? new ApplicationMaterial
        {
            ProfileId = profileId,
            JobId = jobId,
            CoverLetter = string.Empty,
            TailoredCvMarkdown = string.Empty,
            Language = string.Empty,
            SourceCvHash = string.Empty,
            PromptVersion = string.Empty,
            ModelVersion = string.Empty,
            JobVersionHash = string.Empty,
        };

        material.CoverLetter = generated.CoverLetter;
        material.TailoredCvMarkdown = generated.TailoredCvMarkdown;
        material.Language = generated.Language;
        material.SourceCvHash = cv.Hash;
        material.PromptVersion = _generator.PromptVersion;
        material.ModelVersion = _generator.ModelVersion;
        material.JobVersionHash = job.VersionHash;
        material.GeneratedAt = _clock.GetUtcNow();

        await _repo.SaveAsync(material, ct);
        return new MaterialResult(MaterialOutcome.Ready, material);
    }
}
