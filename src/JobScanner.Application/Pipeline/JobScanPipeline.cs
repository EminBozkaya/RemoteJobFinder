using System.Collections.Concurrent;
using JobScanner.Application.Abstractions;
using JobScanner.Application.Deciding;
using JobScanner.Domain.Eligibility;
using JobScanner.Domain.Enums;
using JobScanner.Domain.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobScanner.Application.Pipeline;

/// <summary>
/// Çekirdek tarama akışı: fetch → normalize → dedup → kural elemesi → (fact extraction) →
/// karar + puan. Faz 1'de extraction/decision/scoring stub'tır (LLM yok). Bildirim yok.
/// </summary>
public sealed class JobScanPipeline
{
    private readonly IReadOnlyList<IJobSource> _sources;
    private readonly IJobNormalizer _normalizer;
    private readonly IDeduplicator _dedup;
    private readonly IJobRepository _jobs;
    private readonly IRuleFilter _ruleFilter;
    private readonly IEligibilityExtractor _extractor;
    private readonly IFactsCache _factsCache;
    private readonly IProfileRepository _profiles;
    private readonly IEligibilityDecider _decider;
    private readonly IScoringEngine _scoring;
    private readonly IUserMatchRepository _matches;
    private readonly IExtractionVersion _version;
    private readonly IJobLivenessChecker _liveness;
    private readonly PipelineOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<JobScanPipeline> _log;

    public JobScanPipeline(
        IEnumerable<IJobSource> sources,
        IJobNormalizer normalizer,
        IDeduplicator dedup,
        IJobRepository jobs,
        IRuleFilter ruleFilter,
        IEligibilityExtractor extractor,
        IFactsCache factsCache,
        IProfileRepository profiles,
        IEligibilityDecider decider,
        IScoringEngine scoring,
        IUserMatchRepository matches,
        IExtractionVersion version,
        IJobLivenessChecker liveness,
        IOptions<PipelineOptions> options,
        TimeProvider clock,
        ILogger<JobScanPipeline> log)
    {
        _sources = sources.ToList();
        _normalizer = normalizer;
        _dedup = dedup;
        _jobs = jobs;
        _ruleFilter = ruleFilter;
        _extractor = extractor;
        _factsCache = factsCache;
        _profiles = profiles;
        _decider = decider;
        _scoring = scoring;
        _matches = matches;
        _version = version;
        _liveness = liveness;
        _options = options.Value;
        _clock = clock;
        _log = log;
    }

    public async Task<RunMetrics> RunAsync(CancellationToken ct)
    {
        var runStart = _clock.GetUtcNow();

        // 1. Fetch — paralel + kaynak başına try-catch (biri patlarsa diğerleri sürer; Polly HTTP katmanında)
        var (raw, sourceErrors) = await FetchAllSourcesParallelAsync(ct);

        // 2. Normalize (çekirdek + JSONB + IdentityKey + VersionHash)
        //    Aynı run içinde tekrar eden kimlikleri tekilleştir.
        var normalized = raw
            .Select(_normalizer.Normalize)
            .GroupBy(j => (j.SourceName, j.ExternalId))
            .Select(g => g.First())
            .ToList();

        var activeProfiles = await _profiles.GetActiveAsync(ct);

        int newOrChanged = 0, unchanged = 0, eliminated = 0, extracted = 0, matched = 0, extractionErrors = 0, llmCalls = 0, deadLinks = 0;

        foreach (var job in normalized)
        {
            ct.ThrowIfCancellationRequested();

            // 3. Dedup: New / Unchanged / Changed
            var dr = await _dedup.ClassifyAsync(job, ct);
            JobPosting persisted;
            if (dr.Kind == DedupKind.Unchanged)
            {
                await _jobs.TouchLastSeenAsync(job.SourceName, job.ExternalId, job.LastSeenAt, ct);
                var existing = dr.Existing!;
                // Faz 5b: içerik değişmese de PromptVersion/ModelVersion değiştiyse (cache'te güncel
                // facts yoksa) ilanı yeniden çıkar — model/prompt yükseltmesi tüm ilanlara yansısın.
                var cached = await _factsCache.GetAsync(
                    existing.Id, _version.PromptVersion, _version.ModelVersion, existing.VersionHash, ct);
                if (cached is not null)
                {
                    unchanged++;
                    continue; // güncel facts var → pahalı işi ATLA
                }
                persisted = existing; // yeniden çıkarıma devam
            }
            else
            {
                persisted = await _jobs.UpsertAsync(job, ct);
                newOrChanged++;
            }

            // 4. Ucuz tipli kural elemesi (profil bazlı; forbidden keyword'ler profilden gelir).
            //    Tüm aktif profiller eliyorsa pahalı extraction'a hiç girme.
            var ruleByProfile = activeProfiles.ToDictionary(p => p.Id, p => _ruleFilter.Evaluate(persisted, p));
            if (ruleByProfile.Values.All(r => r.Decision == FilterDecision.Eliminate))
            {
                eliminated++;
                continue;
            }

            // 4.5 Liveness gate: ilan URL'si açıkça öldüyse (404/410) LLM'i çağırma.
            //     Belirsiz durumlar (null) extraction'a devam (Polly + ihtiyatlı yaklaşım).
            var alive = await _liveness.IsAliveAsync(persisted.Url, ct);
            if (alive == false)
            {
                deadLinks++;
                await _jobs.ArchiveOneAsync(persisted.Id, ct);
                _log.LogInformation("İlan {JobId} ({Url}) ölü görünüyor — arşivlendi, extraction atlanıyor", persisted.Id, persisted.Url);
                continue;
            }

            // 5. Fact extraction — cache (JobId+PromptV+ModelV+VersionHash) yoksa extractor çağır.
            //    LLM çağrı hatası bir ilanı atlar ama run'ı düşürmez; çağrı sayısı sınırlanır.
            EligibilityFacts? facts = await _factsCache.GetAsync(
                persisted.Id, _version.PromptVersion, _version.ModelVersion, persisted.VersionHash, ct);

            if (facts is null)
            {
                if (llmCalls >= _options.MaxLlmCallsPerRun)
                {
                    _log.LogInformation("MaxLlmCallsPerRun ({Max}) doldu; kalan ilanların çıkarımı sonraki run'a kaldı", _options.MaxLlmCallsPerRun);
                    break;
                }

                try
                {
                    facts = await ExtractAndCacheAsync(persisted, ct);
                    llmCalls++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    extractionErrors++;
                    _log.LogError(ex, "İlan {JobId} için fact extraction başarısız; bu ilan atlanıyor", persisted.Id);
                    continue;
                }
            }

            extracted++;

            // 5.5 Legitimacy (ghost-job detection, saf C#) — facts + job zaman damgalarından.
            var legit = Legitimacy.Evaluate(facts, persisted, _clock.GetUtcNow());

            // 6. Her aktif profil için: KARAR + PUAN (saf C#, token yok)
            foreach (var profile in activeProfiles)
            {
                if (ruleByProfile[profile.Id].Decision == FilterDecision.Eliminate) continue; // bu profil için elendi
                if (await _matches.IsClosedAsync(profile.Id, persisted.Id, ct)) continue;

                var (decision, reasons) = _decider.Decide(facts, profile);
                if (decision == Decision.Ineligible) continue; // profil bazında uygunsuz

                var score = _scoring.Score(persisted, facts, profile);
                if (score.Final < profile.MinScoreToShow) continue; // eşik altı gösterilmez

                await _matches.UpsertAsync(
                    profile.Id, persisted.Id, score.Final,
                    SerializeBreakdown(score), decision, SerializeReasons(reasons),
                    legit.Confidence, SerializeSignals(legit.Signals), ct);
                matched++;
            }
        }

        // 7. Yaşam döngüsü: bayatlamış ilanları arşivle, açık eşleşmelerini Expired yap.
        var staleSince = runStart.AddDays(-_options.StaleAfterDays);
        var archivedJobs = await _jobs.ArchiveStaleAsync(staleSince, ct);
        var expiredMatches = archivedJobs > 0 ? await _matches.ExpireOpenMatchesForArchivedJobsAsync(ct) : 0;

        var metrics = new RunMetrics(raw.Count, newOrChanged, unchanged, eliminated, extracted, matched, sourceErrors, extractionErrors, archivedJobs, expiredMatches, deadLinks);
        _log.LogInformation(
            "Run metrics: fetched={Fetched} new+changed={NewOrChanged} unchanged={Unchanged} eliminated={Eliminated} extracted={Extracted} matches={Matches} sourceErrors={SourceErrors} extractionErrors={ExtractionErrors} archived={ArchivedJobs} expired={ExpiredMatches} deadLinks={DeadLinks}",
            metrics.Fetched, metrics.NewOrChanged, metrics.Unchanged, metrics.Eliminated, metrics.Extracted, metrics.Matches, metrics.SourceErrors, metrics.ExtractionErrors, metrics.ArchivedJobs, metrics.ExpiredMatches, metrics.DeadLinks);

        return metrics;
    }

    private async Task<(IReadOnlyList<RawJob> Raw, int Errors)> FetchAllSourcesParallelAsync(CancellationToken ct)
    {
        var bag = new ConcurrentBag<RawJob>();
        var errors = 0;
        var query = new SourceQuery(_options.Tags, _options.Geo, _options.Count);

        await Parallel.ForEachAsync(
            _sources,
            new ParallelOptions { MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism, CancellationToken = ct },
            async (source, token) =>
            {
                try
                {
                    var jobs = await source.FetchAsync(query, token);
                    foreach (var j in jobs) bag.Add(j);
                    _log.LogInformation("Source {Source} fetched {Count} jobs", source.Name, jobs.Count);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Interlocked.Increment(ref errors);
                    _log.LogError(ex, "Source {Source} failed; continuing with other sources", source.Name);
                }
            });

        return (bag.ToList(), errors);
    }

    private async Task<EligibilityFacts> ExtractAndCacheAsync(JobPosting job, CancellationToken ct)
    {
        var facts = await _extractor.ExtractAsync(job, ct);
        await _factsCache.SetAsync(facts, ct);
        return facts;
    }

    private static string SerializeReasons(IReadOnlyList<string> reasons) =>
        System.Text.Json.JsonSerializer.Serialize(reasons);

    private static string SerializeBreakdown(JobScore score) =>
        System.Text.Json.JsonSerializer.Serialize(score.Breakdown);

    private static string SerializeSignals(IReadOnlyList<string> signals) =>
        System.Text.Json.JsonSerializer.Serialize(signals);
}
