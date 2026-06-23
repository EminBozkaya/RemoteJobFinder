using JobScanner.Domain.Enums;

namespace JobScanner.Domain.Users;

/// <summary>Kullanıcının bildiği bir yabancı dil + seviyesi (Faz 5b).</summary>
public sealed record LanguageCriterion(string Name, LanguageLevel Level);
