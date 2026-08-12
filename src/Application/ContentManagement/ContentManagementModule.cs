using Microsoft.Extensions.DependencyInjection;

namespace Application.ContentManagement;

public static class ContentManagementModule
{
    public static IServiceCollection AddContentManagementApplication(this IServiceCollection services)
    {
        services.AddScoped<ContentPageService>();
        return services;
    }
}
