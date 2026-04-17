namespace MentalMetal.Application.PersonalAccessTokens;

public static class PatScopeValidator
{
    public static readonly HashSet<string> SupportedScopes = ["captures:write"];

    public static (bool IsValid, List<string> UnsupportedScopes) Validate(IEnumerable<string> scopes)
    {
        var unsupported = scopes.Where(s => !SupportedScopes.Contains(s)).ToList();
        return (unsupported.Count == 0, unsupported);
    }
}
