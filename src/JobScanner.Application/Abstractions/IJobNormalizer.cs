using JobScanner.Domain.Jobs;

namespace JobScanner.Application.Abstractions;

/// <summary>Ham ilanı normalize eder; IdentityKey + VersionHash üretir (saf).</summary>
public interface IJobNormalizer
{
    JobPosting Normalize(RawJob raw);
}
