using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;


namespace OrderService.Application
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddApplicationRegistration(this IServiceCollection services, Type startup)
        {
            Assembly assm = Assembly.GetExecutingAssembly();

            services.AddMediatR(assm);
            services.AddAutoMapper(assm);

            return services;
        }
    }
}
