using MentalMetal.Domain.People;

namespace MentalMetal.Application.Captures.AutoExtract;

/// <summary>
/// Resolves raw name strings from AI extraction against the user's Person entities.
/// Resolution strategy (first match wins):
///   1. Exact case-insensitive match on CanonicalName
///   2. Exact case-insensitive match on any Alias
///   3. Fuzzy: raw name is substring of CanonicalName or vice-versa (min 3 chars, unambiguous)
/// </summary>
public sealed class NameResolutionService
{
    /// <summary>
    /// Resolves each raw name to a PersonId. Returns null for unresolvable names.
    /// </summary>
    public Dictionary<string, Guid?> Resolve(
        IReadOnlyList<string> rawNames,
        IReadOnlyList<Person> people)
    {
        var result = new Dictionary<string, Guid?>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawName in rawNames)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                result.TryAdd(rawName, null);
                continue;
            }

            var trimmed = rawName.Trim();
            if (result.ContainsKey(trimmed))
                continue;

            // 1. Exact match on canonical name
            var exactName = people.FirstOrDefault(p =>
                string.Equals(p.Name, trimmed, StringComparison.OrdinalIgnoreCase));
            if (exactName is not null)
            {
                result[trimmed] = exactName.Id;
                continue;
            }

            // 2. Exact match on any alias
            var aliasMatch = people.FirstOrDefault(p =>
                p.Aliases.Any(a => string.Equals(a, trimmed, StringComparison.OrdinalIgnoreCase)));
            if (aliasMatch is not null)
            {
                result[trimmed] = aliasMatch.Id;
                continue;
            }

            // 3. Fuzzy substring match (min 3 chars, must be unambiguous)
            if (trimmed.Length >= 3)
            {
                var fuzzyMatches = people.Where(p =>
                    p.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Contains(p.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (fuzzyMatches.Count == 1)
                {
                    result[trimmed] = fuzzyMatches[0].Id;
                    continue;
                }
            }

            result[trimmed] = null;
        }

        return result;
    }
}
