namespace JobScanner.Infrastructure.Llm;

/// <summary>
/// LLM yapılandırması (IOptions ile bağlanır). Sağlayıcı-bağımsız; Provider hangi IChatClient
/// factory'sinin seçileceğini belirler. Secret'lar env/user-secrets ile gelir.
/// </summary>
public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>false ise LLM yok: stub extractor (Unknown facts) kullanılır.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>ollama | openai (genişletilebilir).</summary>
    public string Provider { get; init; } = "ollama";

    public string Endpoint { get; init; } = "http://localhost:11434";
    public string Model { get; init; } = "llama3.1";
    public string? ApiKey { get; init; }

    /// <summary>Prompt sürümü (cache anahtarının parçası). Prompt değişince artır.</summary>
    public string PromptVersion { get; init; } = "v4";

    public int MaxOutputTokens { get; init; } = 800;

    /// <summary>Cache anahtarındaki ModelVersion: sağlayıcı + model (model değişince cache geçersizleşir).</summary>
    public string ModelVersion => Enabled ? $"{Provider}/{Model}" : "none";
}
