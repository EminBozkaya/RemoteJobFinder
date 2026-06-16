namespace JobScanner.Application.Pipeline;

/// <summary>Pipeline yapılandırması (IOptions ile bağlanır). Magic string yok.</summary>
public sealed class PipelineOptions
{
    public const string SectionName = "Pipeline";

    public int IntervalHours { get; init; } = 8;
    public int MaxDegreeOfParallelism { get; init; } = 4;

    /// <summary>true ise tek tarama yapip cikar (dedup/kabul testi icin); false ise zamanlanmis dongu.</summary>
    public bool RunOnce { get; init; } = false;

    /// <summary>Fact extraction sürüm anahtarı (cache anahtarının parçası).</summary>
    public string PromptVersion { get; init; } = "v1";
    public string ModelVersion { get; init; } = "stub";

    /// <summary>Faz 1: tek seed source için sorgu parametreleri.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
    public string? Geo { get; init; }
    public int Count { get; init; } = 50;
}
