namespace MentalMetal.Infrastructure.Ai;

public sealed class AiTasteBudget
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public int OperationsUsed { get; set; }
}
