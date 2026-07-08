using CodeClash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeClash.Infrastructure.Persistence.Configurations;

public class BattleRecordConfiguration : IEntityTypeConfiguration<BattleRecord>
{
    public void Configure(EntityTypeBuilder<BattleRecord> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OpponentName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ProblemName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Language).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Duration).HasMaxLength(50).IsRequired();

        // Foreign Key
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
