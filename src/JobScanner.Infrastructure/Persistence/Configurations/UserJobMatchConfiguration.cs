using JobScanner.Domain.Enums;
using JobScanner.Domain.Matching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScanner.Infrastructure.Persistence.Configurations;

internal sealed class UserJobMatchConfiguration : IEntityTypeConfiguration<UserJobMatch>
{
    public void Configure(EntityTypeBuilder<UserJobMatch> b)
    {
        b.ToTable("user_job_matches");
        b.HasKey(x => new { x.ProfileId, x.JobId });

        b.Property(x => x.Decision).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.State).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.Legitimacy).HasConversion<string>().HasMaxLength(32)
            .HasDefaultValueSql("'High'")
            // High = CLR default(0); sentinel enum dışı bir değer olmalı, yoksa EF "High" atamasını
            // "değer verilmedi" sanıp DB default'una bırakır (ve model doğrulama uyarısı verir).
            .HasSentinel((LegitimacyConfidence)(-1));
        b.Property(x => x.LegitimacySignalsJson).HasColumnType("jsonb").HasDefaultValue("[]");
        b.Property(x => x.ScoreBreakdownJson).HasColumnType("jsonb").HasDefaultValue("[]");
        b.Property(x => x.DecisionReasonsJson).HasColumnType("jsonb").HasDefaultValue("[]");
        b.Property(x => x.Feedback).HasMaxLength(32);

        b.HasOne<Domain.Jobs.JobPosting>()
            .WithMany()
            .HasForeignKey(x => x.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.JobId);
        b.HasIndex(x => x.State);
    }
}
