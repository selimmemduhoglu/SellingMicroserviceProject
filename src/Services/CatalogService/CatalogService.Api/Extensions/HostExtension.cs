using Microsoft.EntityFrameworkCore;
using Polly;
using System.Data.SqlClient;

namespace CatalogService.Api.Extensions
{
    public static class HostExtension
    {
        public static IWebHost MigrateDbContext<TContext>(this IWebHost host, Action<TContext, IServiceProvider> seeder)
            where TContext : DbContext
        {
            using (IServiceScope scope = host.Services.CreateScope())
            {
                IServiceProvider services = scope.ServiceProvider;

                ILogger<TContext> logger = services.GetRequiredService<ILogger<TContext>>();

                TContext context = services.GetService<TContext>();

                try
                {
                    logger.LogInformation("Migrating database associated with context {DbContextName}", typeof(TContext).Name);

                    var retry = Policy.Handle<SqlException>()
                             .WaitAndRetry(new TimeSpan[]
                             {
                                 TimeSpan.FromSeconds(3),
                                 TimeSpan.FromSeconds(5),
                                 TimeSpan.FromSeconds(8),
                             });

                    retry.Execute(() => InvokeSeeder(seeder, context, services));

                    logger.LogInformation("Migrated database associated with context {DbContextName}", typeof(TContext).Name);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while migrating the database used on context {DbContextName}", typeof(TContext).Name);
                }
            }

            return host;
        }

        private static void InvokeSeeder<TContext>(Action<TContext, IServiceProvider> seeder, TContext context, IServiceProvider services)
            where TContext : DbContext
        {
            context.Database.EnsureCreated(); // veri tabanının noluşturulup oluşturlmadığı bilgisi sağlayacak ve oluşturulmadıysa oluşturulacak.
            context.Database.Migrate(); // Migration çalışıtırılmadıysa çalıştırılacak.
            seeder(context, services);
        }
    }
}
