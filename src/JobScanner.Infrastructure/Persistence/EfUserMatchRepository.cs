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
        }

        await _db.SaveChangesAsync(ct);
    }
}
