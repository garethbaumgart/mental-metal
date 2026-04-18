using MentalMetal.Domain.Initiatives;

namespace MentalMetal.Application.Captures.AutoExtract;

/// <summary>
/// Fuzzy-matches extracted initiative/project names against existing Initiative titles.
/// </summary>
public sealed class InitiativeTaggingService
{
    /// <summary>
    /// For each raw tag name, attempts to resolve to an existing Initiative.
    /// Uses case-insensitive contains matching with a minimum of 3 characters.
    /// Only resolves when exactly one initiative matches (unambiguous).
    /// </summary>
    public Dictionary<string, Guid?> Resolve(
        IReadOnlyList<string> rawTagNames,
        IReadOnlyList<Initiative> initiatives)
    {
        var result = new Dictionary<string, Guid?>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawName in rawTagNames)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                result.TryAdd(rawName, null);
                continue;
            }

            var trimmed = rawName.Trim();
            if (result.ContainsKey(trimmed))
                continue;

            if (trimmed.Length < 3)
            {
                result[trimmed] = null;
                continue;
            }

            // Case-insensitive contains in either direction
            var matches = initiatives.Where(i =>
                i.Title.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains(i.Title, StringComparison.OrdinalIgnoreCase))
                .ToList();

            result[trimmed] = matches.Count == 1 ? matches[0].Id : null;
        }

        return result;
    }
}
