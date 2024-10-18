namespace CatalogService.Api.Configurations;
public interface IServiceInstaller
{
    void Install(IServiceInstaller serviceInstaller, IConfiguration configuration);
}
