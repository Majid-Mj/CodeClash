using CodeClash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeClash.Infrastructure.Persistence.Configurations;

public class BattleConfiguration : IEntityTypeConfiguration<Battle>
{
    public void Configure(EntityTypeBuilder<Battle> builder)
    {
        builder.ToTable("Battles");

        builder.HasKey(b => b.Id);
        
        builder.HasOne(b => b.Problem)
            .WithMany()
            .HasForeignKey(b => b.ProblemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Winner)
            .WithMany()
            .HasForeignKey(b => b.WinnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Participants)
            .WithOne(p => p.Battle)
            .HasForeignKey(p => p.BattleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
