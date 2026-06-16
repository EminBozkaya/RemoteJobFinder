using JobScanner.Application.Abstractions;
using JobScanner.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace JobScanner.Infrastructure.Persistence;

public sealed class EfProfileRepository : IProfileRepository
{
    private readonly JobScannerDbContext _db;

    public EfProfileRepository(JobScannerDbContext db) => _db = db;

    public async Task<IReadOnlyList<CriteriaProfile>> GetActiveAsync(CancellationToken ct) =>
        await _db.CriteriaProfiles
            .AsNoTracking()
            .Where(p => p.IsActive)
            .ToListAsync(ct);
}
