using Application.Identity;
using Application.Catalog;
using Application.Orders;
using Application.Payments;
using Application.SalesCatalog;
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
            services.AddSalesCatalogModule();
            return services;
        }
    }
}
