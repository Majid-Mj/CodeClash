using CodeClash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeClash.Infrastructure.Persistence.Configurations;

public class TournamentRegistrationConfiguration : IEntityTypeConfiguration<TournamentRegistration>
{
    public void Configure(EntityTypeBuilder<TournamentRegistration> builder)
    {
        builder.ToTable("TournamentRegistrations");
        builder.HasKey(tr => tr.Id);

        // Ensure a user can only register once per tournament
        builder.HasIndex(tr => new { tr.TournamentId, tr.UserId }).IsUnique();

        builder.HasOne(tr => tr.User)
            .WithMany() // Assuming User doesn't have a TournamentRegistrations collection for now
            .HasForeignKey(tr => tr.UserId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent deleting user if registered
    }
}
