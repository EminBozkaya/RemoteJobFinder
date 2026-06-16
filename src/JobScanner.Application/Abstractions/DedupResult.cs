using JobScanner.Domain.Jobs;

namespace JobScanner.Application.Abstractions;

public enum DedupKind { New, Unchanged, Changed }

/// <summary>
/// Bir ilanın daha önce görülüp görülmediğini ve içeriğinin değişip değişmediğini sınıflar.
/// </summary>
public sealed record DedupResult(DedupKind Kind, JobPosting? Existing);
