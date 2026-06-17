using JobScanner.Application.Deciding;
using JobScanner.Domain.Enums;
using Microsoft.Extensions.Options;

namespace JobScanner.UnitTests;

public sealed class EligibilityDeciderTests
{
    private static readonly EligibilityDecider Sut =
        new(Options.Create(new DeciderOptions { MinConfidence = 0.4 }));

    [Fact]
    public void Citizenship_requirement_is_ineligible()
    {
        var (d, _) = Sut.Decide(TestFactory.Facts(requiresCitizenship: true), TestFactory.Profile());
        Assert.Equal(Decision.Ineligible, d);
    }

    [Fact]
    public void Work_auth_without_residence_allowed_is_ineligible()
    {
        var (d, _) = Sut.Decide(
            TestFactory.Facts(requiresWorkAuth: true, allowedCountries: ["US"]),
            TestFactory.Profile());
        Assert.Equal(Decision.Ineligible, d);
    }

    [Fact]
    public void Allowed_countries_excluding_residence_is_ineligible()
    {
        var (d, _) = Sut.Decide(TestFactory.Facts(allowedCountries: ["US", "UK"]), TestFactory.Profile());
        Assert.Equal(Decision.Ineligible, d);
    }

    [Fact]
    public void Worldwide_is_eligible()
    {
        var (d, _) = Sut.Decide(TestFactory.Facts(allowedCountries: ["Worldwide"]), TestFactory.Profile());
        Assert.Equal(Decision.Eligible, d);
    }

    [Fact]
    public void Allowed_countries_including_turkey_is_eligible()
    {
        var (d, _) = Sut.Decide(TestFactory.Facts(allowedCountries: ["Turkey", "Germany"]), TestFactory.Profile());
        Assert.Equal(Decision.Eligible, d);
    }

    [Fact]
    public void Eu_data_boundary_is_ineligible_for_turkey()
    {
        var (d, _) = Sut.Decide(TestFactory.Facts(dataBoundary: "EU only (GDPR)"), TestFactory.Profile());
        Assert.Equal(Decision.Ineligible, d);
    }

    [Fact]
    public void Low_confidence_is_uncertain()
    {
        var (d, _) = Sut.Decide(TestFactory.Facts(confidence: 0.2), TestFactory.Profile());
        Assert.Equal(Decision.Uncertain, d);
    }

    [Fact]
    public void Eor_mention_alone_is_not_eliminating()
    {
        var (d, reasons) = Sut.Decide(
            TestFactory.Facts(mentionsEor: true, engagementType: EngagementType.Contractor),
            TestFactory.Profile());
        Assert.Equal(Decision.Eligible, d);
        Assert.Contains(reasons, r => r.Contains("EOR"));
    }

    [Fact]
    public void Engagement_type_is_not_an_eligibility_gate()
    {
        // Çalışma türü artık eleme/Uncertain üretmez (B2B şart değil; employee/EOR de uygun).
        var (d, _) = Sut.Decide(
            TestFactory.Facts(allowsB2BContractor: false, engagementType: Domain.Enums.EngagementType.Employee),
            TestFactory.Profile(contractTypes: ["b2b"]));
        Assert.Equal(Decision.Eligible, d);
    }

    [Fact]
    public void Relocation_requirement_is_ineligible()
    {
        var (d, _) = Sut.Decide(TestFactory.Facts(requiresRelocation: true), TestFactory.Profile());
        Assert.Equal(Decision.Ineligible, d);
    }

    [Fact]
    public void Foreign_country_background_check_is_ineligible()
    {
        var (d, _) = Sut.Decide(TestFactory.Facts(backgroundCheckCountry: "UK"), TestFactory.Profile());
        Assert.Equal(Decision.Ineligible, d);
    }

    [Fact]
    public void Own_country_or_generic_background_check_is_eligible()
    {
        var own = Sut.Decide(TestFactory.Facts(backgroundCheckCountry: "Turkey"), TestFactory.Profile());
        var generic = Sut.Decide(TestFactory.Facts(backgroundCheckCountry: null), TestFactory.Profile());
        Assert.Equal(Decision.Eligible, own.Decision);
        Assert.Equal(Decision.Eligible, generic.Decision);
    }

    [Fact]
    public void Eor_is_eligible_and_flagged_as_legal_path()
    {
        var (d, reasons) = Sut.Decide(
            TestFactory.Facts(mentionsEor: true, eorPlatform: "Deel", engagementType: EngagementType.EmployeeViaEor),
            TestFactory.Profile());
        Assert.Equal(Decision.Eligible, d);
        Assert.Contains(reasons, r => r.Contains("EOR"));
    }

    [Fact]
    public void Clean_high_confidence_facts_are_eligible()
    {
        var (d, _) = Sut.Decide(TestFactory.Facts(engagementType: EngagementType.B2B), TestFactory.Profile());
        Assert.Equal(Decision.Eligible, d);
    }
}
