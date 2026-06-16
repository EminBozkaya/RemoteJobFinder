using JobScanner.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace JobScanner.Infrastructure.Llm;

/// <summary>Extraction sürüm anahtarlarını LLM yapılandırmasından türetir (tek kaynak).</summary>
public sealed class ExtractionVersion : IExtractionVersion
{
    public ExtractionVersion(IOptions<LlmOptions> options)
    {
        PromptVersion = options.Value.PromptVersion;
        ModelVersion = options.Value.ModelVersion;
    }

    public string PromptVersion { get; }
    public string ModelVersion { get; }
}
