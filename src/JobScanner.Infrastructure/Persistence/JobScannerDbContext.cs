using JobScanner.Domain.Eligibility;
using JobScanner.Domain.Jobs;
using JobScanner.Domain.Matching;
using JobScanner.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace JobScanner.Infrastructure.Persistence;

public sealed class JobScannerDbContext : DbContext
{
    public JobScannerDbContext(DbContextOptions<JobScannerDbContext> options) : base(options) { }

    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<User> Users => Set<User>();
    public DbSet<CriteriaProfile> CriteriaProfiles => Set<CriteriaProfile>();
    public DbSet<EligibilityFacts> EligibilityFactsCache => Set<EligibilityFacts>();
    public DbSet<UserJobMatch> UserJobMatches => Set<UserJobMatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobScannerDbContext).Assembly);
    }
}
