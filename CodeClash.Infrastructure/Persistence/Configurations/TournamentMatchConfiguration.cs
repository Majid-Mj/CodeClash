using CodeClash.Domain.Entities;
using CodeClash.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeClash.Infrastructure.Persistence.Configurations;

public class TournamentMatchConfiguration : IEntityTypeConfiguration<TournamentMatch>
{
    public void Configure(EntityTypeBuilder<TournamentMatch> builder)
    {
        builder.ToTable("TournamentMatches");
        builder.HasKey(tm => tm.Id);

        builder.Property(tm => tm.Status).IsRequired()
            .HasConversion(s => s.ToString(), s => Enum.Parse<MatchStatus>(s))
            .HasMaxLength(30);

        builder.Property(tm => tm.Round).IsRequired()
            .HasConversion(r => r.ToString(), r => Enum.Parse<RoundType>(r))
            .HasMaxLength(30);

        // Relationships
        builder.HasOne(tm => tm.Player1)
            .WithMany()
            .HasForeignKey(tm => tm.Player1Id)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(tm => tm.Player2)
            .WithMany()
            .HasForeignKey(tm => tm.Player2Id)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(tm => tm.Winner)
            .WithMany()
            .HasForeignKey(tm => tm.WinnerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(tm => tm.AssignedProblem)
            .WithMany()
            .HasForeignKey(tm => tm.AssignedProblemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
