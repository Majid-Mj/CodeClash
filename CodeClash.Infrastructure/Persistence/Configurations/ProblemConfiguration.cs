using CodeClash.Domain.Entities;
using CodeClash.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeClash.Infrastructure.Persistence.Configurations;

public class ProblemConfiguration : IEntityTypeConfiguration<Problem>
{
    public void Configure(EntityTypeBuilder<Problem> builder)
    {
        // ── Table ─────────────────────────────────────────────────────────────
        builder.ToTable("Problems");

        // ── Primary Key ───────────────────────────────────────────────────────
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        // ── Columns ───────────────────────────────────────────────────────────
        builder.Property(p => p.Title)
               .IsRequired()
               .HasMaxLength(150);

        builder.Property(p => p.Slug)
               .IsRequired()
               .HasMaxLength(200);

        // Store enums as strings for DB readability
        builder.Property(p => p.Difficulty)
               .IsRequired()
               .HasConversion(
                   d => d.ToString(),
                   s => Enum.Parse<Difficulty>(s))
               .HasMaxLength(20);

        builder.Property(p => p.Category)
               .IsRequired()
               .HasConversion(
                   c => c.ToString(),
                   s => Enum.Parse<ProblemCategory>(s))
               .HasMaxLength(30);

        builder.Property(p => p.StatementMarkdown)
               .IsRequired()
               .HasColumnType("nvarchar(max)");

        // JSON columns — stored as nvarchar(max), deserialized in Application layer
        builder.Property(p => p.ConstraintsJson)
               .IsRequired()
               .HasColumnType("nvarchar(max)")
               .HasDefaultValue("[]");

        builder.Property(p => p.AllowedLanguagesJson)
               .IsRequired()
               .HasColumnType("nvarchar(max)")
               .HasDefaultValue("[]");

        builder.Property(p => p.TimeLimitMs)
               .IsRequired()
               .HasDefaultValue(2000);

        builder.Property(p => p.MemoryLimitMb)
               .IsRequired()
               .HasDefaultValue(256);

        builder.Property(p => p.IsActive)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(p => p.CreatedByUserId)
               .IsRequired();

        builder.Property(p => p.CreatedAt)
               .IsRequired()
               .HasColumnType("datetime2");

        builder.Property(p => p.UpdatedAt)
               .IsRequired()
               .HasColumnType("datetime2");

        builder.Property(p => p.DeletedAt)
               .HasColumnType("datetime2");

        // ── Indexes ───────────────────────────────────────────────────────────
        builder.HasIndex(p => p.Slug)
               .IsUnique()
               .HasDatabaseName("IX_Problems_Slug");

        builder.HasIndex(p => p.Title)
               .IsUnique()
               .HasDatabaseName("IX_Problems_Title");

        builder.HasIndex(p => p.Difficulty)
               .HasDatabaseName("IX_Problems_Difficulty");

        builder.HasIndex(p => p.Category)
               .HasDatabaseName("IX_Problems_Category");

        builder.HasIndex(p => p.IsActive)
               .HasDatabaseName("IX_Problems_IsActive");

        // Composite index for the most common query — active + difficulty filter
        builder.HasIndex(p => new { p.IsActive, p.Difficulty })
               .HasDatabaseName("IX_Problems_IsActive_Difficulty");

        // Soft delete filter — EF global query filter (excludes deleted records automatically)
        builder.HasQueryFilter(p => p.DeletedAt == null);

        // ── Relationships ─────────────────────────────────────────────────────
        builder.HasMany(p => p.TestCases)
               .WithOne(tc => tc.Problem)
               .HasForeignKey(tc => tc.ProblemId)
               .OnDelete(DeleteBehavior.Cascade);

        // ── Backing field for private List<TestCase> ──────────────────────────
        builder.Navigation(p => p.TestCases)
               .UsePropertyAccessMode(PropertyAccessMode.Field)
               .HasField("_testCases");
    }
}