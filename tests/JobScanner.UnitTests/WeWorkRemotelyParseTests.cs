using JobScanner.Infrastructure.Sources.WeWorkRemotely;

namespace JobScanner.UnitTests;

public sealed class WeWorkRemotelyParseTests
{
    [Theory]
    [InlineData("Storetasker: Senior Shopify Developer", "Storetasker", "Senior Shopify Developer")]
    [InlineData("  Acme :  Lead Engineer  ", "Acme", "Lead Engineer")]
    [InlineData("Single Title No Colon", "", "Single Title No Colon")]
    [InlineData(":no company side", "", "no company side")]
    [InlineData("trailing colon:", "", "trailing colon:")]
    [InlineData("", "", "")]
    public void SplitCompanyAndTitle_handles_common_shapes(string raw, string company, string title)
    {
        var (c, t) = WeWorkRemotelyJobSource.SplitCompanyAndTitle(raw);
        Assert.Equal(company, c);
        Assert.Equal(title, t);
    }

    [Fact]
    public void ExtractExternalId_prefers_guid_when_present()
    {
        var id = WeWorkRemotelyJobSource.ExtractExternalId("urn:wwr:12345", "https://x");
        Assert.Equal("urn:wwr:12345", id);
    }

    [Fact]
    public void ExtractExternalId_falls_back_to_link_last_segment()
    {
        var id = WeWorkRemotelyJobSource.ExtractExternalId(null,
            "https://weworkremotely.com/remote-jobs/storetasker-senior-shopify-developer");
        Assert.Equal("storetasker-senior-shopify-developer", id);
    }

    [Fact]
    public void ExtractExternalId_returns_null_when_both_missing()
    {
        Assert.Null(WeWorkRemotelyJobSource.ExtractExternalId(null, null));
        Assert.Null(WeWorkRemotelyJobSource.ExtractExternalId("", "  "));
    }
}
