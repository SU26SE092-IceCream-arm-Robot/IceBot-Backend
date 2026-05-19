using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<IceBotDbContext>
    {
        public IceBotDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();

            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables();

            var configuration = builder.Build();

            var cs = Environment.GetEnvironmentVariable("ConnectionStrings__IceBot_DB")
                     ?? configuration.GetConnectionString("IceBot_DB");

            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException(
                    "Connection string 'IceBot_DB' not found. " +
                    "Set ENV var ConnectionStrings__IceBot_DB hoặc thêm vào appsettings.*");

            var optionsBuilder = new DbContextOptionsBuilder<IceBotDbContext>()
                .UseNpgsql(cs, sql =>
                {
                    sql.MigrationsAssembly(typeof(IceBotDbContext).Assembly.FullName);
                    sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                });

            return new IceBotDbContext(optionsBuilder.Options);
        }
    }
}
