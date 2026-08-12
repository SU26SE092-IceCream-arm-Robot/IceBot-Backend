using Application.Catalog;
using Application.ContentManagement;
using Application.Dashboard.Queries;
using Application.Devices;
using Application.EdgeIntegration;
using Application.EdgeIntegration.Dispatch;
using Application.EdgeIntegration.Reports;
using Application.Identity;
using Application.Inventory;
using Application.Operations;
using Application.Orders;
using Application.Payments;
using Application.ProductionConfiguration;
using Application.ProductionPackages;
using Application.RobotConfiguration;
using Application.SalesCatalog;
using Application.ServiceRegistration;
using Application.Tenants;
using Application.Sync;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddCatalogModule();
            services.AddContentManagementApplication();
            services.AddDevicesModule();
            services.AddSyncModule();
            services.AddEdgeIntegrationModule();
            services.AddIdentityApplication();
            services.AddInventoryModule();
            services.AddOperationsModule();
            services.AddOrderModule();
            services.AddPaymentModule();
            services.AddProductionConfigurationModule();
            services.AddProductionPackagesModule();
            services.AddRobotConfigurationModule();
            services.AddSalesCatalogModule();
            services.AddServiceRegistrationApplication();
            services.AddTenantsApplication();
            services.AddScoped<GetManagementDashboardQueryHandler>();
            return services;
        }
    }
}
