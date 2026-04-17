using System.Collections.Frozen;

namespace MentalMetal.Application.PersonalAccessTokens;

public static class PatScopeValidator
{
    public static readonly FrozenSet<string> SupportedScopes =
        new HashSet<string> { "captures:write" }.ToFrozenSet();

    public static (bool IsValid, List<string> UnsupportedScopes) Validate(IEnumerable<string>? scopes)
    {
        if (scopes is null)
            return (false, ["(null)"]);
        var unsupported = scopes.Where(s => !SupportedScopes.Contains(s)).ToList();
        return (unsupported.Count == 0, unsupported);
    }
}
