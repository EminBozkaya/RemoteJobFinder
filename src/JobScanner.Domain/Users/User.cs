namespace JobScanner.Domain.Users;

/// <summary>
/// Sistem kullanıcısı. Lokal modda tek seed kullanıcı; çok-kullanıcı Faz 3.
/// </summary>
public sealed class User
{
    public long Id { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    public ICollection<CriteriaProfile> Profiles { get; init; } = new List<CriteriaProfile>();
}
