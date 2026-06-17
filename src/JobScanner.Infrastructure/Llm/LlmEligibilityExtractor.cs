using System.Text.Json;
using JobScanner.Application.Abstractions;
using JobScanner.Domain.Eligibility;
using JobScanner.Domain.Enums;
using JobScanner.Domain.Jobs;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobScanner.Infrastructure.Llm;

/// <summary>
/// LLM tabanlı fact extractor (Microsoft.Extensions.AI IChatClient — sağlayıcı-bağımsız).
/// Yapılandırılmış JSON GERÇEK döndürür; KARAR VERMEZ. Toleranslı JSON parse (regex fallback).
/// </summary>
public sealed class LlmEligibilityExtractor : IEligibilityExtractor
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly IChatClient _chat;
    private readonly IExtractionVersion _version;
    private readonly PromptRegistry _prompts;
    private readonly LlmOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<LlmEligibilityExtractor> _log;

    public LlmEligibilityExtractor(
        IChatClient chat,
        IExtractionVersion version,
        PromptRegistry prompts,
        IOptions<LlmOptions> options,
        TimeProvider clock,
        ILogger<LlmEligibilityExtractor> log)
    {
        _chat = chat;
        _version = version;
        _prompts = prompts;
        _options = options.Value;
        _clock = clock;
        _log = log;
    }

    public async Task<EligibilityFacts> ExtractAsync(JobPosting job, CancellationToken ct)
    {
        var (system, user) = _prompts.GetExtractionPrompt(_options.PromptVersion, _options.Model, job);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, system),
            new(ChatRole.User, user),
        };

        var response = await _chat.GetResponseAsync(messages, new ChatOptions
        {
            Temperature = 0f,
            MaxOutputTokens = _options.MaxOutputTokens,
            ResponseFormat = ChatResponseFormat.Json,
        }, ct);

        var rawText = response.Text ?? string.Empty;
        var json = ExtractJsonObject(rawText)
            ?? throw new InvalidOperationException($"LLM yanıtında JSON bulunamadı (job {job.Id}). Yanıt: {Trim(rawText)}");

        ExtractionResult result;
        try
        {
            result = JsonSerializer.Deserialize<ExtractionResult>(json, JsonOpts)
                     ?? throw new InvalidOperationException("Boş JSON");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"LLM JSON parse hatası (job {job.Id}): {Trim(json)}", ex);
        }

        return Map(job, result, json);
    }

    private EligibilityFacts Map(JobPosting job, ExtractionResult r, string rawJson)
    {
        var engagement = Enum.TryParse<EngagementType>(r.EngagementType, ignoreCase: true, out var et)
            ? et
            : EngagementType.Unknown;

        return new EligibilityFacts(
            JobId: job.Id,
            PromptVersion: _version.PromptVersion,
            ModelVersion: _version.ModelVersion,
            VersionHash: job.VersionHash,
            RequiresWorkAuth: r.RequiresWorkAuth,
            RequiresRelocation: r.RequiresRelocation,
            BackgroundCheckCountry: NullIfBlank(r.BackgroundCheckCountry),
            AllowedCountries: NormalizeCountries(r.AllowedCountries),
            RequiresCitizenship: r.RequiresCitizenship,
            AllowsB2BContractor: r.AllowsB2BContractor,
            EngagementType: engagement,
            MentionsEor: r.MentionsEor,
            EorPlatform: NullIfBlank(r.EorPlatform),
            DataBoundary: NullIfBlank(r.DataBoundary),
            TimezoneRequirementRaw: NullIfBlank(r.TimezoneRequirementRaw),
            IsRecruiterAgency: r.IsRecruiterAgency,
            Confidence: Math.Clamp(r.Confidence ?? 0, 0, 1),
            ExtractedAt: _clock.GetUtcNow(),
            RawJson: rawJson);
    }

    /// <summary>Yanıttaki ilk { ... } bloğunu çıkarır (markdown/çerçeve metnine toleranslı).</summary>
    internal static string? ExtractJsonObject(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start) return null;
        return text.Substring(start, end - start + 1);
    }

    private static IReadOnlyList<string>? NormalizeCountries(List<string>? countries)
    {
        if (countries is null) return null;
        var cleaned = countries.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList();
        return cleaned.Count == 0 ? null : cleaned;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Trim(string s) => s.Length <= 300 ? s : s[..300];
}
