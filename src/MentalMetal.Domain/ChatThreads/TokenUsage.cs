using MentalMetal.Domain.Common;

namespace MentalMetal.Domain.ChatThreads;

public sealed class TokenUsage : ValueObject
{
    public int PromptTokens { get; }
    public int CompletionTokens { get; }

    public TokenUsage(int promptTokens, int completionTokens)
    {
        if (promptTokens < 0) throw new ArgumentOutOfRangeException(nameof(promptTokens));
        if (completionTokens < 0) throw new ArgumentOutOfRangeException(nameof(completionTokens));

        PromptTokens = promptTokens;
        CompletionTokens = completionTokens;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PromptTokens;
        yield return CompletionTokens;
    }
}
