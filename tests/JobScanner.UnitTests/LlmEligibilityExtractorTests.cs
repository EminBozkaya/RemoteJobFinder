using JobScanner.Application.Abstractions;
using JobScanner.Domain.Enums;
using JobScanner.Infrastructure.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JobScanner.UnitTests;

public sealed class LlmEligibilityExtractorTests
{
    private sealed class Version : IExtractionVersion
    {
        public string PromptVersion => "v1";
        public string ModelVersion => "ollama/test";
    }

    private static LlmEligibilityExtractor Build(string canned) =>
        new(
            new FakeChatClient(canned),
            new Version(),
            new PromptRegistry(),
            Options.Create(new LlmOptions { Model = "test" }),
            new FakeClock(DateTimeOffset.UnixEpoch),
            NullLogger<LlmEligibilityExtractor>.Instance);

    [Fact]
    public async Task Parses_clean_json_and_maps_fields()
    {
        var json = """
        {"requiresWorkAuth": true, "allowedCountries": ["Worldwide"], "requiresCitizenship": false,
         "allowsB2BContractor": true, "engagementType": "B2B", "mentionsEor": true, "eorPlatform": "Deel",
         "dataBoundary": null, "timezoneRequirementRaw": "CET overlap", "isRecruiterAgency": false,
         "isLikelyGhost": true, "confidence": 0.8}
        """;
        var facts = await Build(json).ExtractAsync(TestFactory.Job(), CancellationToken.None);

        Assert.True(facts.RequiresWorkAuth);
        Assert.True(facts.IsLikelyGhost);
        Assert.Equal(EngagementType.B2B, facts.EngagementType);
        Assert.Equal("Deel", facts.EorPlatform);
        Assert.Equal("CET overlap", facts.TimezoneRequirementRaw);
        Assert.Equal(0.8, facts.Confidence, 3);
        Assert.Equal("v1", facts.PromptVersion);
        Assert.Equal("ollama/test", facts.ModelVersion);
    }

    [Fact]
    public async Task Tolerates_markdown_fenced_json()
    {
        var canned = "Here is the result:\n```json\n{\"engagementType\":\"Contractor\",\"confidence\":0.5}\n```\nThanks!";
        var facts = await Build(canned).ExtractAsync(TestFactory.Job(), CancellationToken.None);

        Assert.Equal(EngagementType.Contractor, facts.EngagementType);
        Assert.Equal(0.5, facts.Confidence, 3);
    }

    [Fact]
    public async Task Clamps_confidence_and_defaults_unknown_engagement()
    {
        var facts = await Build("{\"engagementType\":\"bogus\",\"confidence\":5}").ExtractAsync(TestFactory.Job(), CancellationToken.None);
        Assert.Equal(EngagementType.Unknown, facts.EngagementType);
        Assert.Equal(1.0, facts.Confidence, 3);
    }

    [Fact]
    public async Task Tolerates_null_minYears_in_requiredExperience()
    {
        // llama3.1 gercek ciktisi: minYears null gelebiliyor. Tum extraction dusmemeli,
        // yalnizca gecersiz oge atilmali.
        var json = """
        {"engagementType":"B2B","confidence":0.7,
         "requiredExperience":[{"skill":"React","minYears":3},
                               {"skill":"Kubernetes","minYears":null},
                               {"skill":null,"minYears":5},
                               {"skill":"  ","minYears":2},
                               {"skill":"Go","minYears":0}]}
        """;
        var facts = await Build(json).ExtractAsync(TestFactory.Job(), CancellationToken.None);

        var req = Assert.Single(facts.RequiredExperience!);
        Assert.Equal("React", req.Skill);
        Assert.Equal(3, req.MinYears);
    }

    [Fact]
    public async Task Tolerates_missing_minYears_field_and_string_numbers()
    {
        var json = """
        {"engagementType":"B2B","confidence":"0.6",
         "requiredExperience":[{"skill":"Azure"},{"skill":"C#","minYears":"4"}]}
        """;
        var facts = await Build(json).ExtractAsync(TestFactory.Job(), CancellationToken.None);

        Assert.Equal(0.6, facts.Confidence, 3);
        var req = Assert.Single(facts.RequiredExperience!);
        Assert.Equal("C#", req.Skill);
        Assert.Equal(4, req.MinYears);
    }

    [Fact]
    public async Task Tolerates_decimal_and_suffixed_minYears()
    {
        var json = """
        {"engagementType":"B2B","requiredExperience":[{"skill":"A","minYears":2.5},{"skill":"B","minYears":"5+"},{"skill":"C","minYears":"many"}]}
        """;
        var facts = await Build(json).ExtractAsync(TestFactory.Job(), CancellationToken.None);
        Assert.Equal(new[] { 3, 5 }, facts.RequiredExperience!.Select(r => r.MinYears).ToArray());
    }

    [Fact]
    public async Task RequiredExperience_is_null_when_all_items_invalid()
    {
        var json = """{"engagementType":"B2B","requiredExperience":[{"skill":"X","minYears":null}]}""";
        var facts = await Build(json).ExtractAsync(TestFactory.Job(), CancellationToken.None);
        Assert.Null(facts.RequiredExperience);
    }

    [Fact]
    public async Task Clamps_minYears_to_upper_bound()
    {
        var json = """{"engagementType":"B2B","requiredExperience":[{"skill":"COBOL","minYears":120}]}""";
        var facts = await Build(json).ExtractAsync(TestFactory.Job(), CancellationToken.None);
        Assert.Equal(50, Assert.Single(facts.RequiredExperience!).MinYears);
    }

    [Fact]
    public async Task Throws_when_no_json_present()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Build("I cannot help with that.").ExtractAsync(TestFactory.Job(), CancellationToken.None));
    }
}
