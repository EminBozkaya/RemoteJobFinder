using JobScanner.Domain.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScanner.Infrastructure.Persistence.Configurations;

internal sealed class JobPostingConfiguration : IEntityTypeConfiguration<JobPosting>
{
    public void Configure(EntityTypeBuilder<JobPosting> b)
    {
        b.ToTable("job_postings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityByDefaultColumn();

        b.Property(x => x.SourceName).IsRequired().HasMaxLength(64);
        b.Property(x => x.ExternalId).IsRequired().HasMaxLength(256);
        b.Property(x => x.IdentityKey).IsRequired().HasMaxLength(512);
        b.Property(x => x.Title).IsRequired();
        b.Property(x => x.Company).IsRequired();
        b.Property(x => x.DescriptionText).IsRequired();
        b.Property(x => x.Url).IsRequired();
        b.Property(x => x.ApplyUrl);

        b.Property(x => x.WorkMode).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);

        b.Property(x => x.VersionHash).IsRequired().HasMaxLength(64);
        b.Property(x => x.SourceExtraJson).HasColumnType("jsonb").HasDefaultValue("{}");

        // Kimlik = (SourceName, ExternalId); capraz-kaynak dedup icin IdentityKey index.
        b.HasIndex(x => new { x.SourceName, x.ExternalId }).IsUnique();
        b.HasIndex(x => x.IdentityKey);
    }
}
