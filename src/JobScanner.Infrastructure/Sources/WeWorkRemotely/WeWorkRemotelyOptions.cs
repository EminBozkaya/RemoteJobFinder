namespace JobScanner.Infrastructure.Sources.WeWorkRemotely;

/// <summary>We Work Remotely (RSS) kaynak ayarlari.</summary>
public sealed class WeWorkRemotelyOptions
{
    public const string SectionName = "Sources:WeWorkRemotely";

    public bool Enabled { get; init; } = true;

    /// <summary>RSS feed URL'si. Varsayilan: remote programming kategorisi.</summary>
    public string FeedUrl { get; init; } = "https://weworkremotely.com/categories/remote-programming-jobs.rss";

    public int MaxResults { get; init; } = 100;
}
