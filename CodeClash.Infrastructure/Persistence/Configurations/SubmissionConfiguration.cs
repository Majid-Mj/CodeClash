using CodeClash.Domain.Entities;
using CodeClash.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace CodeClash.Infrastructure.Persistence.Configurations;

public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        // ── Table ─────────────────────────────────────────────────────────────
        builder.ToTable("Submissions");

        // ── Primary Key ───────────────────────────────────────────────────────
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        // ── Columns ───────────────────────────────────────────────────────────
        builder.Property(s => s.ProblemId)
               .IsRequired();

        builder.Property(s => s.UserId)
               .IsRequired();

        builder.Property(s => s.Language)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(s => s.SourceCode)
               .IsRequired()
               .HasColumnType("nvarchar(max)");

        builder.Property(s => s.Status)
               .IsRequired()
               .HasConversion(
                   v => v.ToString(),
                   s => Enum.Parse<SubmissionStatus>(s))
               .HasMaxLength(30);

        builder.Property(s => s.CompileOutput)
               .HasColumnType("nvarchar(max)");

        builder.Property(s => s.RuntimeOutput)
               .HasColumnType("nvarchar(max)");

        builder.Property(s => s.TestCaseResultsJson)
               .HasColumnType("nvarchar(max)");

        builder.Property(s => s.CreatedAt)
               .IsRequired()
               .HasColumnType("datetime2");

        // ── Relationships ─────────────────────────────────────────────────────
        builder.HasOne(s => s.Problem)
               .WithMany()
               .HasForeignKey(s => s.ProblemId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.User)
               .WithMany()
               .HasForeignKey(s => s.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
