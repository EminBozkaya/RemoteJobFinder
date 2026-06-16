using Microsoft.Extensions.AI;
using OllamaSharp;

namespace JobScanner.Infrastructure.Llm;

/// <summary>
/// Provider'a göre IChatClient üretir (sağlayıcı-bağımsız). Yeni sağlayıcı eklemek bir case
/// + paket eklemek demektir; tek sağlayıcı hard-code edilmez.
/// </summary>
internal static class ChatClientFactory
{
    public static IChatClient Create(LlmOptions options) =>
        options.Provider.Trim().ToLowerInvariant() switch
        {
            "ollama" => new OllamaApiClient(new Uri(options.Endpoint), options.Model),
            _ => throw new NotSupportedException(
                $"LLM provider '{options.Provider}' henüz bağlanmadı. " +
                "Ollama hazır; OpenAI/Anthropic için ilgili Microsoft.Extensions.AI paketini ekleyip buraya case ekleyin."),
        };
}
