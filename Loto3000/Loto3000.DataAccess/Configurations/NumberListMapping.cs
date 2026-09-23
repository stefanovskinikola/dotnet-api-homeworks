using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Loto3000.DataAccess.Configurations;

internal static class NumberListMapping
{
    private static readonly ValueConverter<List<int>, string> Converter = new(
        numbers => JsonSerializer.Serialize(numbers, (JsonSerializerOptions?)null),
        json => JsonSerializer.Deserialize<List<int>>(json, (JsonSerializerOptions?)null)!);

    private static readonly ValueComparer<List<int>> Comparer = new(
        (left, right) => left == right || (left != null && right != null && left.SequenceEqual(right)),
        numbers => numbers.Aggregate(0, (hash, number) => HashCode.Combine(hash, number)),
        numbers => numbers.ToList());

    /// <summary>Stores integer lists as JSON and detects edits to an existing list instance.</summary>
    /// <param name="property">The non-null number-list property.</param>
    public static void Configure(PropertyBuilder<List<int>> property)
    {
        property.HasConversion(Converter).HasMaxLength(128).IsRequired();
        // A deep snapshot is essential: reference comparison alone misses in-place list edits.
        property.Metadata.SetValueComparer(Comparer);
    }
}
