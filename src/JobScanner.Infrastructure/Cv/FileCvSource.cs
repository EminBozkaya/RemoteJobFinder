using System.Security.Cryptography;
using System.Text;
using JobScanner.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobScanner.Infrastructure.Cv;

/// <summary>
/// Ana CV'yi diskten okur (markdown). Dosya yoksa/boşsa null döner; üst katman (MaterialService)
/// bunu "CV eksik" sonucuna çevirir. Hash = içeriğin SHA-256'sı (materyal tazeleme anahtarı).
/// </summary>
public sealed class FileCvSource : ICvSource
{
    private readonly CvOptions _options;
    private readonly ILogger<FileCvSource> _log;

    public FileCvSource(IOptions<CvOptions> options, ILogger<FileCvSource> log)
    {
        _options = options.Value;
        _log = log;
    }

    public async Task<CvDocument?> GetAsync(CancellationToken ct)
    {
        var path = System.IO.Path.GetFullPath(_options.Path);
        if (!File.Exists(path))
        {
            _log.LogWarning("Ana CV dosyası bulunamadı: {Path}. Materyal üretimi için data/cv.md oluşturun.", path);
            return null;
        }

        var markdown = await File.ReadAllTextAsync(path, ct);
        if (string.IsNullOrWhiteSpace(markdown))
        {
            _log.LogWarning("Ana CV dosyası boş: {Path}", path);
            return null;
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(markdown)));
        return new CvDocument(markdown, hash);
    }
}
