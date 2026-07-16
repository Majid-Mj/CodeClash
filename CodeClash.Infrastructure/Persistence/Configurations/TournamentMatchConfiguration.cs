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
            .HasConversion(
                s => s.ToString(),
                s => s == "Upcoming" ? MatchStatus.Scheduled : s == "Live" ? MatchStatus.InProgress : Enum.Parse<MatchStatus>(s))
            .HasMaxLength(30);

        builder.Property(tm => tm.Round).IsRequired()
            .HasConversion(r => r.ToString(), r => Enum.Parse<RoundType>(r))
            .HasMaxLength(30);

        // ── Relationships ─────────────────────────────────────────────────────
        // SQL Server does not allow multiple CASCADE / SET NULL paths from the
        // same parent table (Users) to the same child table. Use NoAction here
        // so EF handles nullification in memory before SaveChanges is called.
        builder.HasOne(tm => tm.Player1)
            .WithMany()
            .HasForeignKey(tm => tm.Player1Id)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(tm => tm.Player2)
            .WithMany()
            .HasForeignKey(tm => tm.Player2Id)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(tm => tm.Winner)
            .WithMany()
            .HasForeignKey(tm => tm.WinnerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(tm => tm.AssignedProblem)
            .WithMany()
            .HasForeignKey(tm => tm.AssignedProblemId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(tm => tm.Battle)
            .WithMany()
            .HasForeignKey(tm => tm.BattleId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
