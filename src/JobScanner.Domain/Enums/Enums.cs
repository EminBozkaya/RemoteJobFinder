namespace JobScanner.Domain.Enums;

public enum WorkMode { Remote }                       // şimdilik tek değer

public enum MatchState { New, Saved, Opened, Applied, Dismissed, Expired }

public enum Decision { Eligible, Ineligible, Uncertain }

public enum EngagementType { Unknown, Employee, Contractor, B2B, Freelance, EmployeeViaEor }

public enum JobStatus { Active, Archived }
