using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.Quotes;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.Quotes;

internal class QuoteSetConfiguration : IEntityTypeConfiguration<QuoteSet>
{
    public void Configure(EntityTypeBuilder<QuoteSet> builder)
    {
        builder.ToTable("quote_set");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.QuoteKey).HasMaxLength(200).IsRequired();
        builder.Property(s => s.OrderId).HasMaxLength(100).IsRequired();
        builder.Property(s => s.OrderItemId).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Mode).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.CbdJson).HasColumnName("cbd").HasColumnType("jsonb").IsRequired();

        builder.HasIndex(s => s.QuoteKey).IsUnique();
        builder.HasIndex(s => s.RequestId).IsUnique();
        builder.HasIndex(s => new { s.OrderId, s.OrderItemId });
    }
}
