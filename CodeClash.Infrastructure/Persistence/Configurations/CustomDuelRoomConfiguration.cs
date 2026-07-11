using CodeClash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeClash.Infrastructure.Persistence.Configurations;

public class CustomDuelRoomConfiguration : IEntityTypeConfiguration<CustomDuelRoom>
{
    public void Configure(EntityTypeBuilder<CustomDuelRoom> builder)
    {
        builder.ToTable("CustomDuelRooms");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RoomCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasMaxLength(50);

        // Host relationship
        builder.HasOne(r => r.HostUser)
            .WithMany()
            .HasForeignKey(r => r.HostUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Friend relationship
        builder.HasOne(r => r.FriendUser)
            .WithMany()
            .HasForeignKey(r => r.FriendUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Problem relationship
        builder.HasOne(r => r.SelectedProblem)
            .WithMany()
            .HasForeignKey(r => r.SelectedProblemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
