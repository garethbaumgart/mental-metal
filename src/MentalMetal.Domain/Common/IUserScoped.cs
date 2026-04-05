namespace MentalMetal.Domain.Common;

public interface IUserScoped
{
    Guid UserId { get; }
}
