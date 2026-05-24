using Application.Identity;
using Application.Catalog;
using Application.Orders;
using Application.Payments;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddCatalogModule();
            services.AddIdentityApplication();
            services.AddOrderModule();
            services.AddPaymentModule();
            return services;
        }
    }
}
