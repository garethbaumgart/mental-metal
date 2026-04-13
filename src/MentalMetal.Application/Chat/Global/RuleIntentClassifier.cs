using System.Text.RegularExpressions;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.People;

namespace MentalMetal.Application.Chat.Global;

/// <summary>
/// Cheap, deterministic classifier. Matches well-known English keyword patterns and
/// resolves person/initiative names against the user's actual records. Returns
/// <see cref="IntentSet.Generic"/> when nothing matches so the orchestrator can decide
/// whether to fall back to AI.
/// </summary>
public sealed class RuleIntentClassifier(
    IPersonRepository people,
    IInitiativeRepository initiatives) : IIntentClassifier
{
    // Date phrases. "Today" etc. fire the corresponding time-window intents directly so
    // that a message like "what's overdue today?" lights both OverdueWork and MyDay.
    private static readonly Regex TodayRx = new(@"\b(today|today's|on my plate)\b", RegexOptions.IgnoreCase);
    private static readonly Regex ThisWeekRx = new(@"\b(this week|this week's|by friday)\b", RegexOptions.IgnoreCase);
    private static readonly Regex OverdueRx = new(@"\b(overdue|late|behind)\b", RegexOptions.IgnoreCase);
    private static readonly Regex CaptureSearchRx = new(@"\b(i (captured|wrote|noted)|what did i (capture|write|note))\b", RegexOptions.IgnoreCase);
    private static readonly Regex InitiativePrefixRx = new(@"\b(status of|how is|update on|progress on)\b", RegexOptions.IgnoreCase);

    public async Task<IntentSet> ClassifyAsync(Guid userId, string userMessage, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return IntentSet.Generic;

        var intents = new HashSet<ChatIntent>();
        var personIds = new List<Guid>();
        var initiativeIds = new List<Guid>();

        if (OverdueRx.IsMatch(userMessage)) intents.Add(ChatIntent.OverdueWork);
        if (TodayRx.IsMatch(userMessage)) intents.Add(ChatIntent.MyDay);
        if (ThisWeekRx.IsMatch(userMessage)) intents.Add(ChatIntent.MyWeek);
        if (CaptureSearchRx.IsMatch(userMessage)) intents.Add(ChatIntent.CaptureSearch);

        // Resolve person and initiative names via direct repository scans. Worth doing in
        // every rule pass because PersonLens / InitiativeStatus are both high-precision once
        // a name match lands. Filter by UserId — defence in depth.
        var allPeople = await people.GetAllAsync(userId, typeFilter: null, includeArchived: false, cancellationToken);
        foreach (var person in allPeople.Where(p => p.UserId == userId))
        {
            if (NameMatches(userMessage, person.Name))
            {
                personIds.Add(person.Id);
                intents.Add(ChatIntent.PersonLens);
            }
        }

        var allInitiatives = await initiatives.GetAllAsync(userId, statusFilter: null, cancellationToken);
        foreach (var initiative in allInitiatives.Where(i => i.UserId == userId))
        {
            if (NameMatches(userMessage, initiative.Title))
            {
                initiativeIds.Add(initiative.Id);
                intents.Add(ChatIntent.InitiativeStatus);
            }
        }

        // "How is X?" / "Status of X?" without a matched entity falls through to Generic so
        // the AI fallback can take a swing.
        if (intents.Count == 0 && InitiativePrefixRx.IsMatch(userMessage))
            return IntentSet.Generic;

        if (intents.Count == 0)
            return IntentSet.Generic;

        return new IntentSet(
            intents.ToList(),
            new EntityHints(personIds, initiativeIds, DateRange: null));
    }

    private static bool NameMatches(string message, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        // Whole-word, case-insensitive match against the message. Splitting on whitespace
        // means partial first-name hits ("Sarah" matches both "Sarah Chen" and "Sarah Patel").
        var trimmed = name.Trim();
        if (Regex.IsMatch(message, $@"\b{Regex.Escape(trimmed)}\b", RegexOptions.IgnoreCase))
            return true;

        // First-name only
        var firstName = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (!string.IsNullOrEmpty(firstName)
            && firstName.Length >= 3
            && Regex.IsMatch(message, $@"\b{Regex.Escape(firstName)}\b", RegexOptions.IgnoreCase))
            return true;

        return false;
    }
}
