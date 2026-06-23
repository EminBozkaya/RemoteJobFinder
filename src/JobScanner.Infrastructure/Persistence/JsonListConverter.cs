using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JobScanner.Infrastructure.Persistence;

/// <summary>
/// IReadOnlyList&lt;T&gt; alanlarını jsonb olarak saklamak için genel converter + comparer
/// (T genelde value-equality'li record: SkillCriterion, LanguageCriterion, SkillRequirement).
/// </summary>
internal static class JsonListConverter
{
    public static ValueConverter<IReadOnlyList<T>, string> Converter<T>() => new(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<List<T>>(v, (JsonSerializerOptions?)null) ?? new List<T>());

    public static ValueComparer<IReadOnlyList<T>> Comparer<T>() => new(
        (a, b) => (a ?? new List<T>()).SequenceEqual(b ?? new List<T>()),
        v => v == null ? 0 : v.Aggregate(0, (h, x) => HashCode.Combine(h, x!.GetHashCode())),
        v => v.ToList());

    public static ValueConverter<IReadOnlyList<T>?, string?> NullableConverter<T>() => new(
        v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => v == null ? null : JsonSerializer.Deserialize<List<T>>(v, (JsonSerializerOptions?)null));

    public static ValueComparer<IReadOnlyList<T>?> NullableComparer<T>() => new(
        (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
        v => v == null ? 0 : v.Aggregate(0, (h, x) => HashCode.Combine(h, x!.GetHashCode())),
        v => v == null ? null : v.ToList());
}
