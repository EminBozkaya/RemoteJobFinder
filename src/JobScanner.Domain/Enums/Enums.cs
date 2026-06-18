namespace JobScanner.Domain.Enums;

public enum WorkMode { Remote }                       // şimdilik tek değer

public enum MatchState { New, Saved, Opened, Applied, Dismissed, Expired }

public enum Decision { Eligible, Ineligible, Uncertain }

public enum EngagementType { Unknown, Employee, Contractor, B2B, Freelance, EmployeeViaEor }

public enum JobStatus { Active, Archived }

/// <summary>
/// İlanın "gerçek/canlı" olma güveni (ghost-job detection katmanı).
/// Karar (Decision) ile bağımsız: Eligible bir ilan da Suspicious olabilir.
/// </summary>
public enum LegitimacyConfidence { High, Caution, Suspicious }
