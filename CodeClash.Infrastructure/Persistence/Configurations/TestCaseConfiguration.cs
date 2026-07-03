using CodeClash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeClash.Infrastructure.Persistence.Configurations;

public class TestCaseConfiguration : IEntityTypeConfiguration<TestCase>
{
    public void Configure(EntityTypeBuilder<TestCase> builder)
    {
        // ── Table ─────────────────────────────────────────────────────────────
        builder.ToTable("TestCases");

        // ── Primary Key ───────────────────────────────────────────────────────
        builder.HasKey(tc => tc.Id);
        builder.Property(tc => tc.Id).ValueGeneratedNever();

        // ── Columns ───────────────────────────────────────────────────────────
        builder.Property(tc => tc.ProblemId)
               .IsRequired();

        builder.Property(tc => tc.Input)
               .IsRequired()
               .HasColumnType("nvarchar(max)");

        builder.Property(tc => tc.ExpectedOutput)
               .IsRequired()
               .HasColumnType("nvarchar(max)");

        builder.Property(tc => tc.IsHidden)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(tc => tc.OrderIndex)
               .IsRequired()
               .HasDefaultValue(0);

        // ── Indexes ───────────────────────────────────────────────────────────
        builder.HasIndex(tc => tc.ProblemId)
               .HasDatabaseName("IX_TestCases_ProblemId");

        builder.HasIndex(tc => new { tc.ProblemId, tc.OrderIndex })
               .HasDatabaseName("IX_TestCases_ProblemId_Order");

        builder.HasIndex(tc => tc.IsHidden)
               .HasDatabaseName("IX_TestCases_IsHidden");

        // Soft delete filter — automatically filter out test cases of deleted problems
        builder.HasQueryFilter(tc => tc.Problem.DeletedAt == null);
    }
}