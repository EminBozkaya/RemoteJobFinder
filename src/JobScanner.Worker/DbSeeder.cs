using JobScanner.Domain.Enums;
using JobScanner.Domain.Users;
using JobScanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobScanner.Worker;

/// <summary>
/// Lokal modda tek seed kullanici + tek profil olusturur (yoksa). Faz 1 — tek kullanici.
/// </summary>
internal static class DbSeeder
{
    public static async Task SeedAsync(JobScannerDbContext db, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(ct)) return;

        var user = new User
        {
            Email = "local@jobscanner.dev",
            DisplayName = "Local Contractor",
            CreatedAt = DateTimeOffset.UtcNow,
            Profiles =
            {
                new CriteriaProfile
                {
                    UserId = 0, // FK iliskiyle atanir
                    Name = "Senior .NET Remote",
                    WorkMode = WorkMode.Remote,
                    ResidenceCountry = "TR",
                    ForbiddenKeywords = ["php", "wordpress", "unpaid", "commission only"],
                    Skills =
                    [
                        new SkillCriterion("c#", 9, 6),
                        new SkillCriterion(".net", 9, 6),
                        new SkillCriterion("azure", 6, 3),
                        new SkillCriterion("react", 6, 2),
                        new SkillCriterion("aws", 5, 2),
                    ],
                    Languages =
                    [
                        new LanguageCriterion("English", LanguageLevel.Advanced),
                        new LanguageCriterion("Turkish", LanguageLevel.Native),
                    ],
                    SoftSkills = ["communication", "teamwork"],
                    ContractTypes = ["b2b", "contractor"],
                    TimezoneToleranceHours = 4,
                    SalaryCurrency = "USD",
                    MinScoreToShow = 5.0,
                    IsActive = true,
                },
            },
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }
}
