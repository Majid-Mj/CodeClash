using CodeClash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeClash.Infrastructure.Persistence.Configurations;

public class ProblemLanguageTemplateConfiguration : IEntityTypeConfiguration<ProblemLanguageTemplate>
{
    public void Configure(EntityTypeBuilder<ProblemLanguageTemplate> builder)
    {
        builder.ToTable("ProblemLanguageTemplates");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Language)
               .IsRequired()
               .HasMaxLength(30);

        // WrapperTemplate and StarterCode can be large multi-language code blocks
        builder.Property(t => t.WrapperTemplate)
               .IsRequired()
               .HasColumnType("nvarchar(max)");

        builder.Property(t => t.StarterCode)
               .IsRequired()
               .HasColumnType("nvarchar(max)");

        // Each problem can only have one template per language
        builder.HasIndex(t => new { t.ProblemId, t.Language })
               .IsUnique()
               .HasDatabaseName("IX_ProblemLanguageTemplates_ProblemId_Language");
    }
}
