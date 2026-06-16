using JobScanner.Application.Deciding;
using Microsoft.Extensions.Options;

namespace JobScanner.GoldSetTests;

/// <summary>
/// Gold-set karar regresyonu: etiketli gerçekler → beklenen karar. Deterministik (LLM yok),
/// her zaman çalışır. Karar mantığı değişirse bu testler kırılır.
/// </summary>
public sealed class DecisionRegressionTests
{
    private static readonly EligibilityDecider Decider =
        new(Options.Create(new DeciderOptions { MinConfidence = 0.4 }));

    public static TheoryData<GoldCase> GoldCases()
    {
        var data = new TheoryData<GoldCase>();
        foreach (var c in GoldSet.Cases()) data.Add(c);
        return data;
    }

    [Theory]
    [MemberData(nameof(GoldCases))]
    public void Decision_matches_gold_label(GoldCase c)
    {
        var (decision, reasons) = Decider.Decide(c.Facts, GoldSet.TrContractorProfile);
        Assert.True(decision == c.Expected,
            $"[{c.Name}] beklenen {c.Expected}, gelen {decision}. Gerekçeler: {string.Join(" | ", reasons)}");
    }

    [Fact]
    public void Gold_set_has_at_least_20_cases()
    {
        Assert.True(GoldSet.Cases().Count() >= 20);
    }
}
