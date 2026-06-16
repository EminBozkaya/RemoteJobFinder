namespace JobScanner.Application.Abstractions;

/// <summary>
/// Bir kaynaktan çekim için sorgu parametreleri. Kaynaklar desteklemedikleri alanları yok sayar.
/// </summary>
public sealed record SourceQuery(
    IReadOnlyList<string> Tags,
    string? Geo,
    int Count);
