using JobScanner.Application.Abstractions;
using JobScanner.Application.Pipeline;
using JobScanner.Infrastructure.Llm;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JobScanner.UnitTests;

public sealed class ExtractionCircuitBreakerTests
{
    private sealed class Version : IExtractionVersion
    {
        public string PromptVersion => "v1";
        public string ModelVersion => "ollama/test";
    }

    /// <summary>Sıradaki yanıtı verir; null → bağlantı hatası fırlatır (Ollama kapalı/model yok).</summary>
    private sealed class ScriptedChatClient(params string?[] script) : IChatClient
    {
        private int _i;
        public int Calls => _i;

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var next = script[Math.Min(_i++, script.Length - 1)];
            if (next is null) throw new HttpRequestException("404 (model yok)");
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, next)));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }

    private const string Ok = """{"engagementType":"B2B","confidence":0.7}""";

    /// <summary>Pipeline döngüsünün extraction/devre-kesici davranışını birebir taklit eder.</summary>
    private static async Task<(int Attempts, int Errors, int Extracted, bool Halted)> Drive(ScriptedChatClient chat, int jobs, int max)
    {
        var extractor = new LlmEligibilityExtractor(chat, new Version(), new PromptRegistry(),
            Options.Create(new LlmOptions { Model = "test" }), new FakeClock(DateTimeOffset.UnixEpoch),
            NullLogger<LlmEligibilityExtractor>.Instance);
        var breaker = new ExtractionCircuitBreaker(max);
        int attempts = 0, errors = 0, extracted = 0;
        for (var i = 0; i < jobs; i++)
        {
            if (breaker.IsOpen) continue;
            attempts++;
            try
            {
                await extractor.ExtractAsync(TestFactory.Job(), CancellationToken.None);
                breaker.RecordSuccess();
                extracted++;
            }
            catch (Exception)
            {
                errors++;
                breaker.RecordFailure();
            }
        }
        return (attempts, errors, extracted, breaker.IsOpen);
    }

    [Fact]
    public async Task Always_failing_stops_after_N_attempts()
    {
        var chat = new ScriptedChatClient((string?)null);
        var r = await Drive(chat, jobs: 84, max: 5);

        Assert.Equal(5, r.Attempts);
        Assert.Equal(5, chat.Calls);
        Assert.Equal(5, r.Errors);
        Assert.True(r.Halted);
    }

    [Fact]
    public async Task Success_between_failures_resets_counter()
    {
        // 4 hata, 1 başarı, 4 hata, 1 başarı ... hiçbir zaman 5 ardışık olmaz.
        var script = new string?[] { null, null, null, null, Ok, null, null, null, null, Ok };
        var r = await Drive(new ScriptedChatClient(script), jobs: 10, max: 5);

        Assert.False(r.Halted);
        Assert.Equal(10, r.Attempts);
        Assert.Equal(2, r.Extracted);
    }

    [Fact]
    public void Warns_only_once_and_zero_disables()
    {
        var b = new ExtractionCircuitBreaker(2);
        Assert.False(b.RecordFailure());
        Assert.True(b.RecordFailure());   // devreyi açan çağrı
        Assert.False(b.RecordFailure());  // tekrar WARN yok
        Assert.True(b.IsOpen);

        var off = new ExtractionCircuitBreaker(0);
        for (var i = 0; i < 100; i++) Assert.False(off.RecordFailure());
        Assert.False(off.IsOpen);
    }
}
