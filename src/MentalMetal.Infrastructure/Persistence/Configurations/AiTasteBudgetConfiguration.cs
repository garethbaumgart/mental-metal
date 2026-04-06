using MentalMetal.Infrastructure.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalMetal.Infrastructure.Persistence.Configurations;

public sealed class AiTasteBudgetConfiguration : IEntityTypeConfiguration<AiTasteBudget>
{
    public void Configure(EntityTypeBuilder<AiTasteBudget> builder)
    {
        builder.ToTable("AiTasteBudgets");

        builder.HasKey(b => b.Id);

        builder.HasIndex(b => new { b.UserId, b.Date })
            .IsUnique();

        builder.Property(b => b.Date);
        builder.Property(b => b.OperationsUsed);
    }
}
