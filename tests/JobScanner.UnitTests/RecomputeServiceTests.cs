using JobScanner.Application.Abstractions;
using JobScanner.Application.Applications;
using JobScanner.Application.Deciding;
using JobScanner.Application.Filtering;
using JobScanner.Application.Scoring;
using JobScanner.Domain.Eligibility;
using JobScanner.Domain.Enums;
using JobScanner.Domain.Jobs;
using JobScanner.Domain.Matching;
using JobScanner.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace JobScanner.UnitTests;

public sealed class RecomputeServiceTests
{
    [Fact]
    public async Task Upserts_qualifying_and_deletes_disqualified_without_llm()
    {
        var clock = new FakeClock(DateTimeOffset.UnixEpoch);
        var job1 = JobWithId(1, "Senior .NET Developer", "We use C# and Azure.");   // uygun
        var job2 = JobWithId(2, "Relocation Role", "Must relocate to Berlin.");      // facts → relocation → Ineligible

        var facts = new Dictionary<long, EligibilityFacts>
        {
            [1] = TestFactory.Facts(engagementType: EngagementType.Employee, confidence: 0.9),
            [2] = TestFactory.Facts(requiresRelocation: true, confidence: 0.9),
        };

        var matches = new FakeMatchRepo();
        var svc = new RecomputeService(
            new FakeJobRepo([job1, job2]),
            new FakeFactsCache(facts),
            new FakeProfileRepo(TestFactory.Profile(required: [".net", "c#"])),
            new RuleFilter(),
            new EligibilityDecider(Options.Create(new DeciderOptions { MinConfidence = 0.4 })),
            new ScoringEngine(clock),
            matches,
            new FakeVersion(),
            clock,
            NullLogger<RecomputeService>.Instance);

        var matched = await svc.RecomputeAllAsync(CancellationToken.None);

        Assert.Equal(1, matched);
        Assert.Contains((1L, 1L), matches.Upserted);     // job1 panele yazıldı
        Assert.Contains((1L, 2L), matches.Deleted);      // job2 (Ineligible) açık eşleşmesi silindi
        Assert.DoesNotContain((1L, 2L), matches.Upserted);
    }

    private static JobPosting JobWithId(long id, string title, string desc) => new()
    {
        Id = id, SourceName = "Jobicy", ExternalId = id.ToString(), IdentityKey = "k",
        Title = title, Company = "Acme", DescriptionText = desc,
        Url = "https://x", VersionHash = "h", FirstSeenAt = DateTimeOffset.UnixEpoch,
    };

    // --- Fakes ---

    private sealed class FakeMatchRepo : IUserMatchRepository
    {
        public List<(long, long)> Upserted { get; } = [];
        public List<(long, long)> Deleted { get; } = [];

        public Task<bool> IsClosedAsync(long p, long j, CancellationToken ct) => Task.FromResult(false);

        public Task UpsertAsync(long p, long j, double score, string sb, Decision d, string dr, LegitimacyConfidence l, string ls, CancellationToken ct)
        {
            Upserted.Add((p, j));
            return Task.CompletedTask;
        }

        public Task DeleteNonTerminalAsync(long p, long j, CancellationToken ct)
        {
            Deleted.Add((p, j));
            return Task.CompletedTask;
        }

        public Task<int> ExpireOpenMatchesForArchivedJobsAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<MatchView>> GetRankedAsync(long? p, double m, int t, string? s, CancellationToken ct) => throw new NotImplementedException();
        public Task<bool> WithMatchAsync(long p, long j, Action<UserJobMatch> mutate, CancellationToken ct) => throw new NotImplementedException();
    }

    private sealed class FakeJobRepo(IReadOnlyList<JobPosting> jobs) : IJobRepository
    {
        public Task<IReadOnlyList<JobPosting>> GetActiveAsync(CancellationToken ct) => Task.FromResult(jobs);
        public Task<JobPosting?> FindByIdAsync(long id, CancellationToken ct) => throw new NotImplementedException();
        public Task<JobPosting?> FindByIdentityAsync(string s, string e, CancellationToken ct) => throw new NotImplementedException();
        public Task<JobPosting> UpsertAsync(JobPosting j, CancellationToken ct) => throw new NotImplementedException();
        public Task TouchLastSeenAsync(string s, string e, DateTimeOffset at, CancellationToken ct) => throw new NotImplementedException();
        public Task<int> ArchiveStaleAsync(DateTimeOffset since, CancellationToken ct) => throw new NotImplementedException();
        public Task ArchiveOneAsync(long id, CancellationToken ct) => throw new NotImplementedException();
    }

    private sealed class FakeFactsCache(Dictionary<long, EligibilityFacts> facts) : IFactsCache
    {
        public Task<EligibilityFacts?> GetAsync(long jobId, string pv, string mv, string vh, CancellationToken ct) =>
            Task.FromResult(facts.TryGetValue(jobId, out var f) ? f : null);
        public Task SetAsync(EligibilityFacts facts, CancellationToken ct) => throw new NotImplementedException();
    }

    private sealed class FakeProfileRepo(CriteriaProfile profile) : IProfileRepository
    {
        public Task<IReadOnlyList<CriteriaProfile>> GetActiveAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<CriteriaProfile>>([profile]);
        public Task<bool> UpdateAsync(long id, ProfileEdit edit, CancellationToken ct) => throw new NotImplementedException();
    }

    private sealed class FakeVersion : IExtractionVersion
    {
        public string PromptVersion => "v4";
        public string ModelVersion => "ollama/test";
    }
}
