using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.Quotes;
using Tenengroup.FactoryCatalog.Domains.Shared;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.Quotes;

internal class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.ToTable("quote");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.QuoteId).HasMaxLength(220).IsRequired();
        builder.Property(q => q.FactoryId).HasMaxLength(20).IsRequired();
        builder.Property(q => q.Reasons).HasColumnType("text[]").IsRequired();
        builder.Property(q => q.MaterialCost).HasPrecision(18, 4);
        builder.Property(q => q.LaborCost).HasPrecision(18, 4);
        builder.Property(q => q.PackingCost).HasPrecision(18, 4);
        builder.Property(q => q.TotalCost).HasPrecision(18, 4);
        builder.Property(q => q.Currency).HasMaxLength(3);
        builder.Property(q => q.SharePct).HasPrecision(5, 2);
        builder.Property(q => q.ComponentsJson).HasColumnName("components").HasColumnType("jsonb").IsRequired();
        builder.Property(q => q.ProcessesJson).HasColumnName("processes").HasColumnType("jsonb").IsRequired();
        builder.Property(q => q.ProductionLogJson).HasColumnName("production_log").HasColumnType("jsonb");
        builder.Property(q => q.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(q => q.QuoteSet)
            .WithMany(s => s.Quotes)
            .HasForeignKey(q => q.QuoteSetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Factory>()
            .WithMany()
            .HasForeignKey(q => q.FactoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Quote>()
            .WithMany()
            .HasForeignKey(q => q.ReproductionOfQuoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(q => q.QuoteId).IsUnique();
        builder.HasIndex(q => new { q.FactoryId, q.Status, q.ProducedAt });
    }
}
