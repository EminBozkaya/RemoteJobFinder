using JobScanner.Application.Abstractions;
using JobScanner.Application.Deciding;
using JobScanner.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace JobScanner.Application.Applications;

/// <summary>
/// Kriter profili değişince (Faz 5a) tüm aktif ilanların kararını/puanını **cache'lenmiş facts'ten**
/// saf C# ile yeniden hesaplar — LLM/token YOK. Artık uygun olmayan ilanların açık (terminal olmayan)
/// eşleşmeleri silinir; Applied/Dismissed (kullanıcı kararı) korunur.
/// </summary>
public sealed class RecomputeService
{
    private readonly IJobRepository _jobs;
    private readonly IFactsCache _factsCache;
    private readonly IProfileRepository _profiles;
    private readonly IRuleFilter _ruleFilter;
    private readonly IEligibilityDecider _decider;
    private readonly IScoringEngine _scoring;
    private readonly IUserMatchRepository _matches;
    private readonly IExtractionVersion _version;
    private readonly TimeProvider _clock;
    private readonly ILogger<RecomputeService> _log;

    public RecomputeService(
        IJobRepository jobs,
        IFactsCache factsCache,
        IProfileRepository profiles,
        IRuleFilter ruleFilter,
        IEligibilityDecider decider,
        IScoringEngine scoring,
        IUserMatchRepository matches,
        IExtractionVersion version,
        TimeProvider clock,
        ILogger<RecomputeService> log)
    {
        _jobs = jobs;
        _factsCache = factsCache;
        _profiles = profiles;
        _ruleFilter = ruleFilter;
        _decider = decider;
        _scoring = scoring;
        _matches = matches;
        _version = version;
        _clock = clock;
        _log = log;
    }

    /// <summary>Tüm aktif ilanları yeniden değerlendirir; oluşan/güncellenen eşleşme sayısını döner.</summary>
    public async Task<int> RecomputeAllAsync(CancellationToken ct)
    {
        var profiles = await _profiles.GetActiveAsync(ct);
        var jobs = await _jobs.GetActiveAsync(ct);
        var now = _clock.GetUtcNow();
        var matched = 0;

        foreach (var job in jobs)
        {
            ct.ThrowIfCancellationRequested();

            // Yalnız cache'te (güncel prompt/model + bu VersionHash) facts varsa değerlendirilebilir.
            var facts = await _factsCache.GetAsync(
                job.Id, _version.PromptVersion, _version.ModelVersion, job.VersionHash, ct);
            if (facts is null) continue;

            var legit = Legitimacy.Evaluate(facts, job, now);

            foreach (var profile in profiles)
            {
                if (await _matches.IsClosedAsync(profile.Id, job.Id, ct)) continue; // Applied/Dismissed korunur

                if (_ruleFilter.Evaluate(job, profile).Decision == FilterDecision.Eliminate)
                {
                    await _matches.DeleteNonTerminalAsync(profile.Id, job.Id, ct);
                    continue;
                }

                var (decision, reasons) = _decider.Decide(facts, profile);
                if (decision == Decision.Ineligible)
                {
                    await _matches.DeleteNonTerminalAsync(profile.Id, job.Id, ct);
                    continue;
                }

                var score = _scoring.Score(job, facts, profile);
                if (score.Final < profile.MinScoreToShow)
                {
                    await _matches.DeleteNonTerminalAsync(profile.Id, job.Id, ct);
                    continue;
                }

                await _matches.UpsertAsync(
                    profile.Id, job.Id, score.Final,
                    SerializeBreakdown(score), decision, SerializeReasons(reasons),
                    legit.Confidence, SerializeSignals(legit.Signals), ct);
                matched++;
            }
        }

        _log.LogInformation("Recompute tamamlandı: {Matched} eşleşme (token harcanmadı)", matched);
        return matched;
    }

    private static string SerializeReasons(IReadOnlyList<string> reasons) =>
        System.Text.Json.JsonSerializer.Serialize(reasons);

    private static string SerializeBreakdown(JobScore score) =>
        System.Text.Json.JsonSerializer.Serialize(score.Breakdown);

    private static string SerializeSignals(IReadOnlyList<string> signals) =>
        System.Text.Json.JsonSerializer.Serialize(signals);
}
