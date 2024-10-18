using CatalogService.Api.Core.Domain;
using CatalogService.Api.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogService.Api.Infrastructure.EntityConfigurations;

public class CatalogBrandEntityTypeConfiguration : IEntityTypeConfiguration<CatalogBrand>
{
    public void Configure(EntityTypeBuilder<CatalogBrand> builder)
    {
        builder.ToTable("CatalogBrand", CatalogContext.DEFAULT_SCHEMA); // BU isimde bir tablo oluştur demek ve schema sının ismide bu olsun dedik.

        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.Id)
            .UseHiLo("catalog_brand_hilo") //Hilo veritabanında otomatik artan sayaç için.
            .IsRequired();

        builder.Property(ci => ci.Brand)
            .IsRequired()
            .HasMaxLength(100);

    }
}
