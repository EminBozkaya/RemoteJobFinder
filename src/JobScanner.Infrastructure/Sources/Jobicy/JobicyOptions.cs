namespace JobScanner.Infrastructure.Sources.Jobicy;

/// <summary>Jobicy kaynak ayarlari (IOptions ile baglanir).</summary>
public sealed class JobicyOptions
{
    public const string SectionName = "Sources:Jobicy";

    public bool Enabled { get; init; } = true;
    public string BaseUrl { get; init; } = "https://jobicy.com/api/v2/remote-jobs";

    /// <summary>Maksimum dondurulecek ilan (Jobicy 1..50).</summary>
    public int Count { get; init; } = 50;
}
