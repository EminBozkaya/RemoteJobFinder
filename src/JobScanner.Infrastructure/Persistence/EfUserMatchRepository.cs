using JobScanner.Application.Abstractions;
using JobScanner.Domain.Enums;
using JobScanner.Domain.Matching;
using Microsoft.EntityFrameworkCore;

namespace JobScanner.Infrastructure.Persistence;

public sealed class EfUserMatchRepository : IUserMatchRepository
{
    private readonly JobScannerDbContext _db;
    private readonly TimeProvider _clock;

    public EfUserMatchRepository(JobScannerDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<bool> IsClosedAsync(long profileId, long jobId, CancellationToken ct) =>
        await _db.UserJobMatches.AsNoTracking().AnyAsync(
            m => m.ProfileId == profileId && m.JobId == jobId &&
                 (m.State == MatchState.Applied || m.State == MatchState.Dismissed), ct);

    public async Task UpsertAsync(
        long profileId,
        long jobId,
        double score,
        string scoreBreakdownJson,
        Decision decision,
        string decisionReasonsJson,
        LegitimacyConfidence legitimacy,
        string legitimacySignalsJson,
        CancellationToken ct)
    {
        var existing = await _db.UserJobMatches
            .FirstOrDefaultAsync(m => m.ProfileId == profileId && m.JobId == jobId, ct);

        if (existing is null)
        {
            _db.UserJobMatches.Add(new UserJobMatch
            {
                ProfileId = profileId,
                JobId = jobId,
                Score = score,
                ScoreBreakdownJson = scoreBreakdownJson,
                Decision = decision,
                DecisionReasonsJson = decisionReasonsJson,
                Legitimacy = legitimacy,
                LegitimacySignalsJson = legitimacySignalsJson,
                State = MatchState.New,
                CreatedAt = _clock.GetUtcNow(),
            });
        }
        else
        {
            // Kullanici durumu (State/Feedback) korunur; yalniz hesaplanan alanlar guncellenir.
            existing.Score = score;
            existing.ScoreBreakdownJson = scoreBreakdownJson;
            existing.Decision = decision;
            existing.DecisionReasonsJson = decisionReasonsJson;
            existing.Legitimacy = legitimacy;
            existing.LegitimacySignalsJson = legitimacySignalsJson;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> ExpireOpenMatchesForArchivedJobsAsync(CancellationToken ct)
    {
        var archivedJobIds = _db.JobPostings
            .Where(j => j.Status == JobStatus.Archived)
            .Select(j => j.Id);

        return await _db.UserJobMatches
            .Where(m => archivedJobIds.Contains(m.JobId) &&
                        (m.State == MatchState.New || m.State == MatchState.Saved || m.State == MatchState.Opened))
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.State, MatchState.Expired), ct);
    }

    public async Task DeleteNonTerminalAsync(long profileId, long jobId, CancellationToken ct) =>
        await _db.UserJobMatches
            .Where(m => m.ProfileId == profileId && m.JobId == jobId &&
                        (m.State == MatchState.New || m.State == MatchState.Saved || m.State == MatchState.Opened))
            .ExecuteDeleteAsync(ct);

    public async Task<IReadOnlyList<MatchView>> GetRankedAsync(long? profileId, double minScore, int take, string? source, CancellationToken ct) =>
        await (
            from m in _db.UserJobMatches.AsNoTracking()
            join j in _db.JobPostings.AsNoTracking() on m.JobId equals j.Id
            where (profileId == null || m.ProfileId == profileId)
                  && m.Score >= minScore
                  && m.State != MatchState.Expired
                  && m.State != MatchState.Dismissed
                  && (source == null || j.SourceName == source)
            orderby m.Score descending, j.PostedAt descending
            select new MatchView(
                m.ProfileId, m.JobId, j.SourceName, j.Title, j.Company, j.Url, j.ApplyUrl,
                m.Score, m.Decision.ToString(), m.State.ToString(), m.Legitimacy.ToString(),
                j.PostedAt,
                m.ScoreBreakdownJson, m.DecisionReasonsJson, m.LegitimacySignalsJson))
            .Take(take)
            .ToListAsync(ct);

    public async Task<bool> WithMatchAsync(long profileId, long jobId, Action<UserJobMatch> mutate, CancellationToken ct)
    {
        var match = await _db.UserJobMatches
            .FirstOrDefaultAsync(m => m.ProfileId == profileId && m.JobId == jobId, ct);

        if (match is null) return false;

        mutate(match);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
