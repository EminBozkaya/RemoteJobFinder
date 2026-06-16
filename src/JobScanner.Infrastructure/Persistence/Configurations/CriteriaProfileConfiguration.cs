using JobScanner.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScanner.Infrastructure.Persistence.Configurations;

internal sealed class CriteriaProfileConfiguration : IEntityTypeConfiguration<CriteriaProfile>
{
    public void Configure(EntityTypeBuilder<CriteriaProfile> b)
    {
        b.ToTable("criteria_profiles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityByDefaultColumn();

        b.Property(x => x.Name).IsRequired().HasMaxLength(256);
        b.Property(x => x.WorkMode).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.ResidenceCountry).IsRequired().HasMaxLength(8);
        b.Property(x => x.SalaryCurrency).HasMaxLength(8);

        ConfigureStringList(b, x => x.RequiredKeywords, "required_keywords");
        ConfigureStringList(b, x => x.ForbiddenKeywords, "forbidden_keywords");
        ConfigureStringList(b, x => x.NiceKeywords, "nice_keywords");
        ConfigureStringList(b, x => x.ContractTypes, "contract_types");

        b.HasIndex(x => x.IsActive);
    }

    private static void ConfigureStringList(
        EntityTypeBuilder<CriteriaProfile> b,
        System.Linq.Expressions.Expression<Func<CriteriaProfile, IReadOnlyList<string>>> property,
        string columnName)
    {
        var p = b.Property(property)
            .HasConversion(StringListConverter.Converter)
            .HasColumnType("jsonb")
            .HasColumnName(columnName);
        p.Metadata.SetValueComparer(StringListConverter.Comparer);
    }
}
