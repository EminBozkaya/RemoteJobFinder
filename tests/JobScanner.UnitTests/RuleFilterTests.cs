using JobScanner.Application.Abstractions;
using JobScanner.Application.Filtering;
using JobScanner.Domain.Enums;
using JobScanner.Domain.Jobs;
using Xunit;

namespace JobScanner.UnitTests;

public sealed class RuleFilterTests
{
    private static readonly RuleFilter Filter = new();

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
    public void Passes_when_no_forbidden_keywords()
    {
        var result = Filter.Evaluate(Job(), TestFactory.Profile(forbidden: []));
        Assert.Equal(FilterDecision.Pass, result.Decision);
    }

    [Fact]
    public void Eliminates_when_forbidden_keyword_in_title()
    {
        var result = Filter.Evaluate(Job(title: "WordPress Engineer"), TestFactory.Profile(forbidden: ["wordpress"]));
        Assert.Equal(FilterDecision.Eliminate, result.Decision);
    }

    [Fact]
    public void Eliminates_when_forbidden_keyword_in_description()
    {
        var result = Filter.Evaluate(Job(description: "This is an unpaid internship."), TestFactory.Profile(forbidden: ["unpaid"]));
        Assert.Equal(FilterDecision.Eliminate, result.Decision);
    }

    [Fact]
    public void Forbidden_match_is_case_insensitive()
    {
        var result = Filter.Evaluate(Job(title: "Senior php Developer"), TestFactory.Profile(forbidden: ["PHP"]));
        Assert.Equal(FilterDecision.Eliminate, result.Decision);
    }

    [Fact]
    public void Required_keywords_do_NOT_eliminate_anymore()
    {
        // Faz 5a: required keyword'ler eleyici değil (scoring sinyali). Eşleşmese bile geçer.
        var result = Filter.Evaluate(Job(title: "Java Developer", description: "Spring Boot"), TestFactory.Profile(required: [".net", "c#"]));
        Assert.Equal(FilterDecision.Pass, result.Decision);
    }

    [Fact]
    public void Eliminates_non_remote_workmode()
    {
        var result = Filter.Evaluate(Job(workMode: (WorkMode)999), TestFactory.Profile());
        Assert.Equal(FilterDecision.Eliminate, result.Decision);
    }
}
