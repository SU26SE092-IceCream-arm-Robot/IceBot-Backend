using Application.Abstractions.Persistence;
using Application.Email;
using Infrastructure.Catalog;
using Infrastructure.Data;
using Infrastructure.Email;
using Infrastructure.Identity;
using Infrastructure.Orders;
using Infrastructure.Payments;
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
            services.AddScoped<Application.Inventory.Abstractions.IInventoryStore, Infrastructure.Inventory.Persistence.InventoryStore>();

            return services;
        }
    }
}
