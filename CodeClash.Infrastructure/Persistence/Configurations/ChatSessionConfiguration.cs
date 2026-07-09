using CodeClash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeClash.Infrastructure.Persistence.Configurations;

public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.LastActiveAt).IsRequired();

        builder.HasMany(s => s.Messages)
               .WithOne(m => m.Session)
               .HasForeignKey(m => m.SessionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => new { s.UserId, s.ProblemId });
    }
}
