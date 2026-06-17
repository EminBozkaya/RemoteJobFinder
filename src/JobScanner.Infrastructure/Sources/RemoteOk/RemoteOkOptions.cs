namespace JobScanner.Infrastructure.Sources.RemoteOk;

/// <summary>RemoteOK kaynak ayarlari (IOptions ile baglanir).</summary>
public sealed class RemoteOkOptions
{
    public const string SectionName = "Sources:RemoteOK";

    public bool Enabled { get; init; } = true;
    public string BaseUrl { get; init; } = "https://remoteok.com/api";

    /// <summary>
    /// Etiket filtrelemesi: bos ise tum donen ilanlar isleme alinir;
    /// dolu ise SourceQuery.Tags'i bu etiketler uzerinden filtre olarak kullanir
    /// (RemoteOK tek seferde tum ilanlari donduruyor; filtre client-side).
    /// </summary>
    public int MaxResults { get; init; } = 100;
}
