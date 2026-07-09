using CodeClash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeClash.Infrastructure.Persistence.Configurations;

public class KnowledgeChunkConfiguration : IEntityTypeConfiguration<KnowledgeChunk>
{
    public void Configure(EntityTypeBuilder<KnowledgeChunk> builder)
    {
        builder.HasKey(k => k.Id);

        builder.Property(k => k.Title)
               .IsRequired()
               .HasMaxLength(300);

        builder.Property(k => k.Content)
               .IsRequired()
               .HasColumnType("nvarchar(max)");

        builder.Property(k => k.EmbeddingJson)
               .IsRequired()
               .HasColumnType("nvarchar(max)");

        builder.Property(k => k.Category).HasMaxLength(100);
    }
}
