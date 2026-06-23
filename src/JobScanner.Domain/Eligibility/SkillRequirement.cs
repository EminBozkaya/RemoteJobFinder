namespace JobScanner.Domain.Eligibility;

/// <summary>
/// İlanın belirli bir yetkinlik için istediği asgari tecrübe yılı (LLM çıkarımı, Faz 5b).
/// Ör. "React, min 3 yıl" → SkillRequirement("React", 3). Kullanıcının yılıyla kıyaslanır.
/// </summary>
public sealed record SkillRequirement(string Skill, int MinYears);
