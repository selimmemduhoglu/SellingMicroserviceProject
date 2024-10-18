namespace CatalogService.Api.Core.Domain;

public class CatalogItem
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Price { get; set; } = default!;
    public string PictureFileName { get; set; } = default!;
    public string PictureUrl { get; set; } = default!;
    public string CatalogTypeId { get; set; } = default!;
    public CatalogType CatalogType { get; set; } = new();
    public string CatalogBrandId { get; set; } = default!;
    public CatalogBrand CatalogBrand { get; set; } = new();
}
