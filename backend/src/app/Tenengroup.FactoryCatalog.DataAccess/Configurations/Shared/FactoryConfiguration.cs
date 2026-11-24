using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tenengroup.FactoryCatalog.Domains.Shared;

namespace Tenengroup.FactoryCatalog.DataAccess.Configurations.Shared;

internal class FactoryConfiguration : IEntityTypeConfiguration<Factory>
{
    public void Configure(EntityTypeBuilder<Factory> builder)
    {
        builder.ToTable("factory");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id).HasMaxLength(20);
        builder.Property(f => f.Name).HasMaxLength(200).IsRequired();
        builder.Property(f => f.CountryCode).HasMaxLength(2);

        builder.HasData(
            new Factory { Id = "110", Name = "Factory 110", IsActive = true },
            new Factory { Id = "125", Name = "Factory 125", IsActive = true },
            new Factory { Id = "315", Name = "Factory 315", IsActive = true });
    }
}
