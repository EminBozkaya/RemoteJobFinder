namespace JobScanner.Application.Abstractions;

/// <summary>
/// Fact extraction sürüm anahtarlarının tek kaynağı. Hem pipeline (cache anahtarı için)
/// hem extractor (ürettiği facts için) aynı değerleri kullanır; aksi halde cache hiç tutmaz.
/// </summary>
public interface IExtractionVersion
{
    string PromptVersion { get; }
    string ModelVersion { get; }
}
