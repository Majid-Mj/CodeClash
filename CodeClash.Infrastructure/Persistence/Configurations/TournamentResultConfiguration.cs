using CodeClash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeClash.Infrastructure.Persistence.Configurations;

public class TournamentResultConfiguration : IEntityTypeConfiguration<TournamentResult>
{
    public void Configure(EntityTypeBuilder<TournamentResult> builder)
    {
        builder.ToTable("TournamentResults");
        builder.HasKey(tr => tr.Id);

        // Ensure one result per user per tournament
        builder.HasIndex(tr => new { tr.TournamentId, tr.UserId }).IsUnique();

        builder.HasOne(tr => tr.User)
            .WithMany()
            .HasForeignKey(tr => tr.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
