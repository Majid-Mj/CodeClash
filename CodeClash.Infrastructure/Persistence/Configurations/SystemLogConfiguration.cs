using CodeClash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeClash.Infrastructure.Persistence.Configurations;

public class SystemLogConfiguration : IEntityTypeConfiguration<SystemLog>
{
    public void Configure(EntityTypeBuilder<SystemLog> builder)
    {
        builder.ToTable("SystemLogs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Level)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.Source)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.Message)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(l => l.CreatedAt)
            .IsRequired();
    }
}
