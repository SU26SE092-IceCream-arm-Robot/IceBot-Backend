using Application.Catalog;
using Application.Dashboard.Queries;
using Application.Devices;
using Application.EdgeIntegration;
using Application.Identity;
using Application.Inventory;
using Application.Operations;
using Application.Orders;
using Application.Payments;
using Application.ProductionConfiguration;
using Application.RobotConfiguration;
using Application.SalesCatalog;
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
            services.AddDevicesModule();
            services.AddScoped<ListSyncDeadLettersQueryHandler>();
            services.AddScoped<GetSyncDeadLetterQueryHandler>();
            services.AddScoped<ResolveSyncDeadLetterCommandHandler>();
            services.AddScoped<IgnoreSyncDeadLetterCommandHandler>();
            services.AddScoped<RetrySyncDeadLetterCommandHandler>();
            services.AddEdgeIntegrationModule();
            services.AddIdentityApplication();
            services.AddInventoryModule();
            services.AddOperationsModule();
            services.AddOrderModule();
            services.AddPaymentModule();
            services.AddProductionConfigurationModule();
            services.AddRobotConfigurationModule();
            services.AddSalesCatalogModule();
            services.AddTenantsApplication();
            services.AddScoped<GetManagementDashboardQueryHandler>();
            return services;
        }
    }
}
