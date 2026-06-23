using JobScanner.Domain.Eligibility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScanner.Infrastructure.Persistence.Configurations;

internal sealed class EligibilityFactsConfiguration : IEntityTypeConfiguration<EligibilityFacts>
{
    public void Configure(EntityTypeBuilder<EligibilityFacts> b)
    {
        b.ToTable("eligibility_facts_cache");

        // Cache anahtari: JobId + PromptVersion + ModelVersion + VersionHash (karar DEGIL).
        b.HasKey(x => new { x.JobId, x.PromptVersion, x.ModelVersion, x.VersionHash });

        b.Property(x => x.PromptVersion).HasMaxLength(32);
        b.Property(x => x.ModelVersion).HasMaxLength(64);
        b.Property(x => x.VersionHash).HasMaxLength(64);
        b.Property(x => x.EngagementType).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.BackgroundCheckCountry).HasMaxLength(64);
        b.Property(x => x.EorPlatform).HasMaxLength(128);
        b.Property(x => x.DataBoundary).HasMaxLength(64);
        b.Property(x => x.TimezoneRequirementRaw).HasMaxLength(256);
        b.Property(x => x.RawJson).HasColumnType("jsonb").HasDefaultValue("{}");

        var p = b.Property(x => x.AllowedCountries)
            .HasConversion(StringListConverter.NullableConverter)
            .HasColumnType("jsonb")
            .HasColumnName("allowed_countries");
        p.Metadata.SetValueComparer(StringListConverter.NullableComparer);

        // Faz 5b: ilanın istediği asgari tecrübe yılları (jsonb, nullable record listesi)
        var re = b.Property(x => x.RequiredExperience)
            .HasConversion(JsonListConverter.NullableConverter<SkillRequirement>())
            .HasColumnType("jsonb")
            .HasColumnName("required_experience");
        re.Metadata.SetValueComparer(JsonListConverter.NullableComparer<SkillRequirement>());
    }
}
