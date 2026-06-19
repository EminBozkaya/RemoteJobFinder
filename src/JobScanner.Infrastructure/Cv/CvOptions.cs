namespace JobScanner.Infrastructure.Cv;

/// <summary>Ana (kaynak) CV ayarları. Dosya kişisel veridir; repoya girmez (gitignore).</summary>
public sealed class CvOptions
{
    public const string SectionName = "Cv";

    /// <summary>Ana CV markdown dosyasının yolu (çalışma dizinine göreli olabilir).</summary>
    public string Path { get; init; } = "data/cv.md";
}
