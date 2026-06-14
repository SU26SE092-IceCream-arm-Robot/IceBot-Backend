using Application.Catalog;
using Application.Dashboard.Queries;
using Application.Devices;
using Application.Identity;
using Application.Inventory;
using Application.Orders;
using Application.Payments;
using Application.SalesCatalog;
using Application.Tenants;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddCatalogModule();
            services.AddDevicesModule();
            services.AddIdentityApplication();
            services.AddInventoryModule();
            services.AddOperationsModule();
            services.AddOrderModule();
            services.AddPaymentModule();
            services.AddSalesCatalogModule();
            services.AddTenantsApplication();
            services.AddScoped<GetManagementDashboardQueryHandler>();
            return services;
        }
    }
}
