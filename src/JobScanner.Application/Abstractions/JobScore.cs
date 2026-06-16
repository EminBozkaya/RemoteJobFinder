namespace JobScanner.Application.Abstractions;

/// <summary>Bir kriterin nihai puana katkısı (açıklanabilirlik için).</summary>
public sealed record ScoreContribution(string Criterion, double Contribution);

/// <summary>Saf C# puanlama çıktısı: nihai puan + kriter bazında dökümü.</summary>
public sealed record JobScore(double Final, IReadOnlyList<ScoreContribution> Breakdown);
