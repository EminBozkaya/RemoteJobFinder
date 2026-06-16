using JobScanner.Application.Abstractions;
using JobScanner.Application.Filtering;
using JobScanner.Domain.Enums;
using JobScanner.Domain.Jobs;
using Microsoft.Extensions.Options;

namespace JobScanner.UnitTests;

public sealed class RuleFilterTests
{
    private static RuleFilter Build(string[]? forbidden = null, string[]? required = null) =>
        new(Options.Create(new RuleFilterOptions
        {
            ForbiddenKeywords = forbidden ?? [],
            RequiredKeywords = required ?? [],
        }));

    private static JobPosting Job(
        string title = "Senior .NET Developer",
        string description = "We use C# and Azure.",
        WorkMode workMode = WorkMode.Remote) =>
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
            WorkMode = workMode,
        };

    [Fact]
    public void Passes_when_no_rules_configured()
    {
        var result = Build().Evaluate(Job());
        Assert.Equal(FilterDecision.Pass, result.Decision);
    }

    [Fact]
    public void Eliminates_when_forbidden_keyword_in_title()
    {
        var result = Build(forbidden: ["wordpress"]).Evaluate(Job(title: "WordPress Engineer"));
        Assert.Equal(FilterDecision.Eliminate, result.Decision);
    }

    [Fact]
    public void Eliminates_when_forbidden_keyword_in_description()
    {
        var result = Build(forbidden: ["unpaid"]).Evaluate(Job(description: "This is an unpaid internship."));
        Assert.Equal(FilterDecision.Eliminate, result.Decision);
    }

    [Fact]
    public void Forbidden_match_is_case_insensitive()
    {
        var result = Build(forbidden: ["PHP"]).Evaluate(Job(title: "Senior php Developer"));
        Assert.Equal(FilterDecision.Eliminate, result.Decision);
    }

    [Fact]
    public void Eliminates_when_required_keywords_none_present()
    {
        var result = Build(required: [".net", "c#"]).Evaluate(Job(title: "Java Developer", description: "Spring Boot"));
        Assert.Equal(FilterDecision.Eliminate, result.Decision);
    }

    [Fact]
    public void Passes_when_at_least_one_required_keyword_present()
    {
        var result = Build(required: [".net", "c#"]).Evaluate(Job(description: "We use C# heavily."));
        Assert.Equal(FilterDecision.Pass, result.Decision);
    }

    [Fact]
    public void Eliminates_non_remote_workmode()
    {
        // WorkMode su an tek deger (Remote); enum disi degerle eleme yolu dogrulanir.
        var job = Job(workMode: (WorkMode)999);
        var result = Build().Evaluate(job);
        Assert.Equal(FilterDecision.Eliminate, result.Decision);
    }
}
