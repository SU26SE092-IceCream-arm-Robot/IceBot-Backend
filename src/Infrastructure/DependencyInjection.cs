using Application.Abstractions.Persistence;
using Application.Dashboard.Abstractions;
using Application.Devices.Abstractions;
using Application.Email;
using Application.Inventory.Abstractions;
using Application.Operations.Abstractions;
using Application.EdgeIntegration.Abstractions;
using Application.ProductionConfiguration.Abstractions;
using Application.RobotConfiguration.Abstractions;
using Infrastructure.Catalog;
using Infrastructure.Dashboard.Persistence;
using Infrastructure.Data;
using Infrastructure.Devices.Persistence;
using Infrastructure.Email;
using Infrastructure.EdgeIntegration.Persistence;
using Infrastructure.Identity;
using Infrastructure.Inventory.Persistence;
using Infrastructure.Operations.Persistence;
using Infrastructure.Orders;
using Infrastructure.Payments;
using Infrastructure.ProductionConfiguration.Persistence;
using Infrastructure.RobotConfiguration.ObjectStorage;
using Infrastructure.RobotConfiguration.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.SalesCatalog;
using Infrastructure.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<IceBotDbContext>((sp, opt) =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();

                var cs = configuration["CONNECTIONSTRING"];

                if (string.IsNullOrWhiteSpace(cs))
                    cs = configuration.GetConnectionString("IceBot_DB");

                if (string.IsNullOrWhiteSpace(cs))
                    throw new InvalidOperationException(
                        "Missing DB connection string. Set CONNECTIONSTRING or ConnectionStrings:IceBot_DB.");

                opt.UseNpgsql(cs);
            });

            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            services.Configure<EmailOptions>(config.GetSection(EmailOptions.SectionName));
            services.AddScoped<IEmailSender, MailKitEmailSender>();
            services.AddCatalogInfrastructure();
            services.AddIdentityInfrastructure();
            services.AddOrdersInfrastructure();
            services.AddPaymentsInfrastructure(config);
            services.AddSalesCatalogInfrastructure();
            services.AddTenantsInfrastructure();
            services.AddScoped<IInventoryStore, InventoryStore>();
            services.AddHostedService<Persistence.Jobs.DataRetentionJob>();
            services.AddScoped<IKioskTelemetryStore, KioskTelemetryStore>();
            services.AddScoped<IDeviceManagementStore, DeviceManagementStore>();
            services.AddScoped<IExecutionEndpointStore, ExecutionEndpointStore>();
            services.AddScoped<IDashboardStore, DashboardStore>();
            services.AddScoped<IMaintenanceTicketStore, MaintenanceTicketStore>();
            services.Configure<RobotArtifactObjectStorageOptions>(config.GetSection(RobotArtifactObjectStorageOptions.SectionName));
            services.AddScoped<IArtifactObjectStorage, MinioArtifactObjectStorage>();
            services.AddScoped<IRobotConfigurationStore, RobotConfigurationStore>();
            services.AddScoped<IProductionConfigurationStore, ProductionConfigurationStore>();
            services.AddScoped<IEdgeCommandStore, EdgeCommandStore>();
            services.AddScoped<IExecutionReportStore, ExecutionReportStore>();

            return services;
        }
    }
}
