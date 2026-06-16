namespace JobScanner.UnitTests;

/// <summary>Test icin sabit zaman dondurur.</summary>
internal sealed class FakeClock(DateTimeOffset now) : TimeProvider
{
    private readonly DateTimeOffset _now = now;
    public override DateTimeOffset GetUtcNow() => _now;
}
