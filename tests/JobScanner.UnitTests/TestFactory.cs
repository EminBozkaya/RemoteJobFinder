using JobScanner.Domain.Eligibility;
using JobScanner.Domain.Enums;
using JobScanner.Domain.Jobs;
using JobScanner.Domain.Users;

namespace JobScanner.UnitTests;

/// <summary>Testler için varsayılan domain nesneleri üretir.</summary>
internal static class TestFactory
{
    public static EligibilityFacts Facts(
        bool? requiresWorkAuth = null,
        bool? requiresRelocation = null,
        string? backgroundCheckCountry = null,
        IReadOnlyList<string>? allowedCountries = null,
        bool? requiresCitizenship = null,
        bool? allowsB2BContractor = null,
        EngagementType engagementType = EngagementType.Unknown,
        bool? mentionsEor = null,
        string? eorPlatform = null,
        string? dataBoundary = null,
        string? timezoneRequirementRaw = null,
        bool? isRecruiterAgency = null,
        bool? isLikelyGhost = null,
        double confidence = 0.9,
        IReadOnlyList<SkillRequirement>? requiredExperience = null) =>
        new(
            JobId: 1, PromptVersion: "v1", ModelVersion: "test", VersionHash: "h",
            RequiresWorkAuth: requiresWorkAuth, RequiresRelocation: requiresRelocation,
            BackgroundCheckCountry: backgroundCheckCountry,
            AllowedCountries: allowedCountries,
            RequiresCitizenship: requiresCitizenship, AllowsB2BContractor: allowsB2BContractor,
            EngagementType: engagementType, MentionsEor: mentionsEor, EorPlatform: eorPlatform,
            DataBoundary: dataBoundary, TimezoneRequirementRaw: timezoneRequirementRaw,
            IsRecruiterAgency: isRecruiterAgency, IsLikelyGhost: isLikelyGhost,
            Confidence: confidence, ExtractedAt: DateTimeOffset.UnixEpoch, RawJson: "{}",
            RequiredExperience: requiredExperience);

    public static CriteriaProfile Profile(
        string residenceCountry = "TR",
        IReadOnlyList<SkillCriterion>? skills = null,
        IReadOnlyList<string>? forbidden = null,
        IReadOnlyList<LanguageCriterion>? languages = null,
        IReadOnlyList<string>? softSkills = null,
        IReadOnlyList<string>? contractTypes = null,
        int timezoneToleranceHours = 4) =>
        new()
        {
            Id = 1,
            UserId = 1,
            Name = "Test",
            ResidenceCountry = residenceCountry,
            Skills = skills ?? [],
            ForbiddenKeywords = forbidden ?? [],
            Languages = languages ?? [],
            SoftSkills = softSkills ?? [],
            ContractTypes = contractTypes ?? ["b2b", "contractor"],
            TimezoneToleranceHours = timezoneToleranceHours,
        };

    public static JobPosting Job(
        string title = "Senior .NET Developer",
        string description = "We use C# and Azure.",
        DateTimeOffset? postedAt = null) =>
        new()
        {
            SourceName = "Jobicy",
            ExternalId = "1",
            IdentityKey = "k",
            Title = title,
            Company = "Acme",
            DescriptionText = description,
            Url = "https://x",
            VersionHash = "h",
            PostedAt = postedAt,
            FirstSeenAt = postedAt ?? DateTimeOffset.UnixEpoch,
        };
}
