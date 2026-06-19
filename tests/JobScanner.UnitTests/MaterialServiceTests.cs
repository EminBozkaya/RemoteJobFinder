using JobScanner.Application.Abstractions;
using JobScanner.Application.Applications;
using JobScanner.Domain.Applications;
using JobScanner.Domain.Jobs;
using JobScanner.Domain.Users;
using Xunit;

namespace JobScanner.UnitTests;

public sealed class MaterialServiceTests
{
    private static MaterialService Build(
        FakeMaterialGenerator generator,
        FakeMaterialRepo repo,
        bool jobExists = true,
        bool cvExists = true)
    {
        var jobs = new FakeJobRepo(jobExists ? JobWithHash("h") : null);
        var profiles = new FakeProfileRepo(TestFactory.Profile());
        var cvSource = new FakeCvSource(cvExists ? new CvDocument("# CV", "cvhash") : null);
        return new MaterialService(jobs, profiles, cvSource, generator, repo, new FakeClock(DateTimeOffset.UnixEpoch));
    }

    private static JobPosting JobWithHash(string hash) => new()
    {
        Id = 1, SourceName = "Jobicy", ExternalId = "1", IdentityKey = "k",
        Title = "Senior .NET Developer", Company = "Acme", DescriptionText = "We use C#.",
        Url = "https://x", VersionHash = hash, FirstSeenAt = DateTimeOffset.UnixEpoch,
    };

    [Fact]
    public async Task Generates_and_persists_when_no_existing_material()
    {
        var gen = new FakeMaterialGenerator();
        var repo = new FakeMaterialRepo();
        var svc = Build(gen, repo);

        var result = await svc.GetOrGenerateAsync(1, 1, forceRegenerate: false, CancellationToken.None);

        Assert.Equal(MaterialOutcome.Ready, result.Outcome);
        Assert.Equal(1, gen.CallCount);
        Assert.NotNull(repo.Saved);
        Assert.Equal("cover", result.Material!.CoverLetter);
        Assert.Equal("en", result.Material.Language);
    }

    [Fact]
    public async Task Returns_cached_without_calling_llm_when_fresh()
    {
        var gen = new FakeMaterialGenerator();
        var repo = new FakeMaterialRepo();
        repo.Seed(Fresh(cvHash: "cvhash", prompt: gen.PromptVersion, model: gen.ModelVersion, jobHash: "h"));
        var svc = Build(gen, repo);

        var result = await svc.GetOrGenerateAsync(1, 1, forceRegenerate: false, CancellationToken.None);

        Assert.Equal(MaterialOutcome.Ready, result.Outcome);
        Assert.Equal(0, gen.CallCount); // token harcanmadı
    }

    [Fact]
    public async Task Regenerates_when_base_cv_changed()
    {
        var gen = new FakeMaterialGenerator();
        var repo = new FakeMaterialRepo();
        repo.Seed(Fresh(cvHash: "OLD", prompt: gen.PromptVersion, model: gen.ModelVersion, jobHash: "h"));
        var svc = Build(gen, repo); // güncel cv hash = "cvhash"

        var result = await svc.GetOrGenerateAsync(1, 1, forceRegenerate: false, CancellationToken.None);

        Assert.Equal(MaterialOutcome.Ready, result.Outcome);
        Assert.Equal(1, gen.CallCount);
    }

    [Fact]
    public async Task Force_regenerates_even_when_fresh()
    {
        var gen = new FakeMaterialGenerator();
        var repo = new FakeMaterialRepo();
        repo.Seed(Fresh(cvHash: "cvhash", prompt: gen.PromptVersion, model: gen.ModelVersion, jobHash: "h"));
        var svc = Build(gen, repo);

        var result = await svc.GetOrGenerateAsync(1, 1, forceRegenerate: true, CancellationToken.None);

        Assert.Equal(MaterialOutcome.Ready, result.Outcome);
        Assert.Equal(1, gen.CallCount);
    }

    [Fact]
    public async Task Returns_JobNotFound_when_job_missing()
    {
        var svc = Build(new FakeMaterialGenerator(), new FakeMaterialRepo(), jobExists: false);
        var result = await svc.GetOrGenerateAsync(1, 999, false, CancellationToken.None);
        Assert.Equal(MaterialOutcome.JobNotFound, result.Outcome);
    }

    [Fact]
    public async Task Returns_CvMissing_when_no_base_cv()
    {
        var svc = Build(new FakeMaterialGenerator(), new FakeMaterialRepo(), cvExists: false);
        var result = await svc.GetOrGenerateAsync(1, 1, false, CancellationToken.None);
        Assert.Equal(MaterialOutcome.CvMissing, result.Outcome);
    }

    [Fact]
    public async Task Returns_LlmDisabled_when_generator_unavailable_and_no_fresh_cache()
    {
        var svc = Build(new FakeMaterialGenerator { Available = false }, new FakeMaterialRepo());
        var result = await svc.GetOrGenerateAsync(1, 1, false, CancellationToken.None);
        Assert.Equal(MaterialOutcome.LlmDisabled, result.Outcome);
    }

    private static ApplicationMaterial Fresh(string cvHash, string prompt, string model, string jobHash) => new()
    {
        ProfileId = 1, JobId = 1,
        CoverLetter = "cached", TailoredCvMarkdown = "cached-cv", Language = "tr",
        SourceCvHash = cvHash, PromptVersion = prompt, ModelVersion = model, JobVersionHash = jobHash,
        GeneratedAt = DateTimeOffset.UnixEpoch,
    };

    // --- Fakes ---

    private sealed class FakeMaterialGenerator : IApplicationMaterialGenerator
    {
        public bool Available { get; init; } = true;
        public string PromptVersion => "mv1";
        public string ModelVersion => "ollama/test";
        public int CallCount { get; private set; }

        public Task<GeneratedMaterials> GenerateAsync(JobPosting job, CriteriaProfile profile, string baseCvMarkdown, CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult(new GeneratedMaterials("cover", "cv-md", "en"));
        }
    }

    private sealed class FakeMaterialRepo : IApplicationMaterialRepository
    {
        private ApplicationMaterial? _stored;
        public ApplicationMaterial? Saved { get; private set; }
        public void Seed(ApplicationMaterial m) => _stored = m;

        public Task<ApplicationMaterial?> GetAsync(long profileId, long jobId, CancellationToken ct) => Task.FromResult(_stored);

        public Task SaveAsync(ApplicationMaterial material, CancellationToken ct)
        {
            _stored = material;
            Saved = material;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeJobRepo(JobPosting? job) : IJobRepository
    {
        public Task<JobPosting?> FindByIdAsync(long id, CancellationToken ct) => Task.FromResult(job);
        public Task<JobPosting?> FindByIdentityAsync(string sourceName, string externalId, CancellationToken ct) => throw new NotImplementedException();
        public Task<JobPosting> UpsertAsync(JobPosting j, CancellationToken ct) => throw new NotImplementedException();
        public Task TouchLastSeenAsync(string sourceName, string externalId, DateTimeOffset seenAt, CancellationToken ct) => throw new NotImplementedException();
        public Task<int> ArchiveStaleAsync(DateTimeOffset notSeenSince, CancellationToken ct) => throw new NotImplementedException();
        public Task ArchiveOneAsync(long jobId, CancellationToken ct) => throw new NotImplementedException();
    }

    private sealed class FakeProfileRepo(CriteriaProfile profile) : IProfileRepository
    {
        public Task<IReadOnlyList<CriteriaProfile>> GetActiveAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<CriteriaProfile>>([profile]);
    }

    private sealed class FakeCvSource(CvDocument? cv) : ICvSource
    {
        public Task<CvDocument?> GetAsync(CancellationToken ct) => Task.FromResult(cv);
    }
}
