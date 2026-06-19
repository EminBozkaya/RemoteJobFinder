using System.Text.Json;
using System.Text.Json.Serialization;
using JobScanner.Application.Abstractions;
using JobScanner.Domain.Applications;
using JobScanner.Domain.Jobs;
using JobScanner.Domain.Users;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobScanner.Infrastructure.Llm;

/// <summary>
/// LLM tabanlı başvuru materyali üreticisi (Microsoft.Extensions.AI IChatClient — sağlayıcı-bağımsız).
/// Tek çağrıda yapılandırılmış JSON döndürür: { language, coverLetter, tailoredCvMarkdown }.
/// Faz 4: LLM burada ÜRETİM yapar; karar/puan hâlâ saf C#'tadır.
/// </summary>
public sealed class LlmApplicationMaterialGenerator : IApplicationMaterialGenerator
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly IChatClient _chat;
    private readonly MaterialPromptRegistry _prompts;
    private readonly LlmOptions _options;
    private readonly ILogger<LlmApplicationMaterialGenerator> _log;

    public LlmApplicationMaterialGenerator(
        IChatClient chat,
        MaterialPromptRegistry prompts,
        IOptions<LlmOptions> options,
        ILogger<LlmApplicationMaterialGenerator> log)
    {
        _chat = chat;
        _prompts = prompts;
        _options = options.Value;
        _log = log;
    }

    public bool Available => true;
    public string PromptVersion => _options.MaterialPromptVersion;
    public string ModelVersion => _options.ModelVersion;

    public async Task<GeneratedMaterials> GenerateAsync(
        JobPosting job, CriteriaProfile profile, string baseCvMarkdown, CancellationToken ct)
    {
        var (system, user) = _prompts.GetMaterialPrompt(_options.MaterialPromptVersion, job, profile, baseCvMarkdown);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, system),
            new(ChatRole.User, user),
        };

        var response = await _chat.GetResponseAsync(messages, new ChatOptions
        {
            Temperature = 0.4f,
            MaxOutputTokens = _options.MaterialMaxOutputTokens,
            ResponseFormat = ChatResponseFormat.Json,
        }, ct);

        var rawText = response.Text ?? string.Empty;
        var json = LlmEligibilityExtractor.ExtractJsonObject(rawText)
            ?? throw new InvalidOperationException($"LLM materyal yanıtında JSON bulunamadı (job {job.Id}).");

        MaterialDto dto;
        try
        {
            dto = JsonSerializer.Deserialize<MaterialDto>(json, JsonOpts)
                  ?? throw new InvalidOperationException("Boş JSON");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"LLM materyal JSON parse hatası (job {job.Id}).", ex);
        }

        var coverLetter = (dto.CoverLetter ?? string.Empty).Trim();
        var cv = (dto.TailoredCvMarkdown ?? string.Empty).Trim();
        if (coverLetter.Length == 0 && cv.Length == 0)
            throw new InvalidOperationException($"LLM boş materyal döndürdü (job {job.Id}).");

        var language = string.IsNullOrWhiteSpace(dto.Language) ? "en" : dto.Language.Trim().ToLowerInvariant();
        _log.LogInformation("Materyal üretildi: job {JobId}, dil {Language}", job.Id, language);

        return new GeneratedMaterials(coverLetter, cv, language);
    }

    private sealed record MaterialDto(
        [property: JsonPropertyName("language")] string? Language,
        [property: JsonPropertyName("coverLetter")] string? CoverLetter,
        [property: JsonPropertyName("tailoredCvMarkdown")] string? TailoredCvMarkdown);
}
