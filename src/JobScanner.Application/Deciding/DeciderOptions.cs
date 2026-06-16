namespace JobScanner.Application.Deciding;

/// <summary>Uygunluk kararı eşikleri (config-driven).</summary>
public sealed class DeciderOptions
{
    public const string SectionName = "Decider";

    /// <summary>Bu değerin altındaki Confidence → Uncertain (sert eleme yoksa).</summary>
    public double MinConfidence { get; init; } = 0.4;
}
