using CodeClash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeClash.Infrastructure.Persistence.Configurations;

public class BattleParticipantConfiguration : IEntityTypeConfiguration<BattleParticipant>
{
    public void Configure(EntityTypeBuilder<BattleParticipant> builder)
    {
        builder.ToTable("BattleParticipants");

        builder.HasKey(bp => bp.Id);

        builder.HasIndex(bp => new { bp.BattleId, bp.UserId }).IsUnique();

        builder.HasOne(bp => bp.User)
            .WithMany()
            .HasForeignKey(bp => bp.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
