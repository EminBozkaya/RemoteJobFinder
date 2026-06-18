using System.Net;
using JobScanner.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace JobScanner.Infrastructure.Liveness;

/// <summary>
/// HEAD request ile bir ilan URL'sinin canlı olup olmadığını kontrol eder.
/// HEAD desteklemeyen sunucular için GET fallback (yalnız header okuma).
/// Hata/timeout durumunda 'null' döner — pipeline ihtiyatlı olur ve devam eder.
/// </summary>
public sealed class HttpLivenessChecker : IJobLivenessChecker
{
    private readonly HttpClient _http;
    private readonly ILogger<HttpLivenessChecker> _log;

    public HttpLivenessChecker(HttpClient http, ILogger<HttpLivenessChecker> log)
    {
        _http = http;
        _log = log;
    }

    public async Task<bool?> IsAliveAsync(string url, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        try
        {
            using var resp = await SendAsync(uri, HttpMethod.Head, ct);
            // 405 Method Not Allowed → HEAD desteklemiyor, GET ile dene
            if (resp.StatusCode == HttpStatusCode.MethodNotAllowed)
            {
                using var get = await SendAsync(uri, HttpMethod.Get, ct);
                return InterpretStatus(get.StatusCode);
            }
            return InterpretStatus(resp.StatusCode);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LogDebug(ex, "Liveness check başarısız ({Url}); 'belirsiz' kabul edilip devam ediliyor", url);
            return null;
        }
    }

    private async Task<HttpResponseMessage> SendAsync(Uri uri, HttpMethod method, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, uri);
        return await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    /// <summary>200/3xx → true (canlı). 404/410 → false (ölü). Diğer → null (belirsiz).</summary>
    private static bool? InterpretStatus(HttpStatusCode code) => code switch
    {
        HttpStatusCode.NotFound or HttpStatusCode.Gone => false,
        _ when (int)code is >= 200 and < 400 => true,
        _ => null,
    };
}
