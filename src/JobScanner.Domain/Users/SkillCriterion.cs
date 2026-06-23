namespace JobScanner.Domain.Users;

/// <summary>
/// Kullanıcının bir teknik yetkinliği. Öz-puan (1-10) puanlamaya, tecrübe yılı ise ilanın
/// istediği yılla kıyasa (yumuşak ceza) beslenir. Faz 5b.
/// </summary>
public sealed record SkillCriterion(string Name, int SelfRating, int Years);
