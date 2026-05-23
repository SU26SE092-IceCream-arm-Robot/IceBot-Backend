using Application.Identity;
using Application.Payments;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddIdentityApplication();
            services.AddPaymentModule();
            return services;
        }
    }
}
