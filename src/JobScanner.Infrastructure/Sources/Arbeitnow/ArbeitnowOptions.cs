namespace JobScanner.Infrastructure.Sources.Arbeitnow;

/// <summary>
/// Arbeitnow kaynak ayarlari. API sunucu-tarafi filtrelemiyor; client-side filtre uygularz.
/// Visa sponsorship flag onemli sinyal (EU sirketlerinin TR'li adaylara cikip cikamayacagini gosterir).
/// </summary>
public sealed class ArbeitnowOptions
{
    public const string SectionName = "Sources:Arbeitnow";

    public bool Enabled { get; init; } = true;
    public string BaseUrl { get; init; } = "https://www.arbeitnow.com/api/job-board-api";

    public int MaxResults { get; init; } = 100;

    /// <summary>true ise yalniz 'remote: true' isaretli ilanlar isleme alinir (Arbeitnow karisik feed).</summary>
    public bool RemoteOnly { get; init; } = true;
}
