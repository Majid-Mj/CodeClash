using CodeClash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeClash.Infrastructure.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Role)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(m => m.Content)
               .IsRequired()
               .HasColumnType("nvarchar(max)");

        builder.Property(m => m.CreatedAt).IsRequired();

        builder.HasIndex(m => m.SessionId);
    }
}
