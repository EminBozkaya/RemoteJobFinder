using JobScanner.Domain.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScanner.Infrastructure.Persistence.Configurations;

internal sealed class ApplicationMaterialConfiguration : IEntityTypeConfiguration<ApplicationMaterial>
{
    public void Configure(EntityTypeBuilder<ApplicationMaterial> b)
    {
        b.ToTable("application_materials");
        b.HasKey(x => new { x.ProfileId, x.JobId });

        b.Property(x => x.Language).HasMaxLength(8);
        b.Property(x => x.SourceCvHash).HasMaxLength(64);
        b.Property(x => x.PromptVersion).HasMaxLength(32);
        b.Property(x => x.ModelVersion).HasMaxLength(64);
        b.Property(x => x.JobVersionHash).HasMaxLength(64);
        // CoverLetter / TailoredCvMarkdown: uzun metin (text, sınırsız)

        b.HasOne<Domain.Jobs.JobPosting>()
            .WithMany()
            .HasForeignKey(x => x.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.JobId);
    }
}
