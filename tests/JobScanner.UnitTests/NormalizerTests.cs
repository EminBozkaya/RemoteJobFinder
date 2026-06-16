using JobScanner.Domain.Jobs;
using JobScanner.Infrastructure.Normalization;

namespace JobScanner.UnitTests;

public sealed class NormalizerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);
    private readonly Normalizer _sut = new(new FakeClock(Now));

    private static RawJob Raw(
        string title = "Senior .NET Developer",
        string company = "Acme Corp",
        string descriptionHtml = "<p>Build <b>cool</b> things.</p>",
        string url = "https://jobs.example/1",
        string? applyUrl = null) =>
        new("Jobicy", "123", title, company, descriptionHtml, url, applyUrl, "2026-06-15T10:00:00Z",
            new Dictionary<string, object?> { ["jobGeo"] = "anywhere" });

    [Fact]
    public void HtmlToText_strips_tags_and_collapses_whitespace()
    {
        var text = Normalizer.HtmlToText("<p>Hello   <b>world</b></p>\n<div>  again </div>");
        Assert.Equal("Hello world again", text);
    }

    [Fact]
    public void HtmlToText_empty_returns_empty()
    {
        Assert.Equal(string.Empty, Normalizer.HtmlToText(null));
        Assert.Equal(string.Empty, Normalizer.HtmlToText("   "));
    }

    [Fact]
    public void BuildIdentityKey_is_lowercased_company_pipe_title()
    {
        var key = Normalizer.BuildIdentityKey("Acme Corp", "Senior .NET Developer");
        Assert.Equal("acme corp|senior .net developer", key);
    }

    [Fact]
    public void Normalize_sets_identity_versionhash_and_seen_timestamps()
    {
        var job = _sut.Normalize(Raw());

        Assert.Equal("Jobicy", job.SourceName);
        Assert.Equal("123", job.ExternalId);
        Assert.Equal("acme corp|senior .net developer", job.IdentityKey);
        Assert.Equal("Build cool things.", job.DescriptionText);
        Assert.Equal(Now, job.FirstSeenAt);
        Assert.Equal(Now, job.LastSeenAt);
        Assert.False(string.IsNullOrWhiteSpace(job.VersionHash));
        Assert.Equal(64, job.VersionHash.Length); // SHA-256 hex
    }

    [Fact]
    public void VersionHash_is_stable_for_same_content()
    {
        var a = _sut.Normalize(Raw());
        var b = _sut.Normalize(Raw());
        Assert.Equal(a.VersionHash, b.VersionHash);
    }

    [Fact]
    public void VersionHash_changes_when_description_changes()
    {
        var a = _sut.Normalize(Raw(descriptionHtml: "<p>Original</p>"));
        var b = _sut.Normalize(Raw(descriptionHtml: "<p>Changed</p>"));
        Assert.NotEqual(a.VersionHash, b.VersionHash);
    }

    [Fact]
    public void Normalize_parses_posted_at_as_utc()
    {
        var job = _sut.Normalize(Raw());
        Assert.Equal(new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero), job.PostedAt);
    }

    [Fact]
    public void Normalize_blank_apply_url_becomes_null()
    {
        var job = _sut.Normalize(Raw(applyUrl: "   "));
        Assert.Null(job.ApplyUrl);
    }
}
