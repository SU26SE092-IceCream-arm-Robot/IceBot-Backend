using Application.ClientDevices;
using Application.Tenants.Kiosks.Rules;

namespace WebAPI.Configuration.Hosting;

public static class ClientDeviceRuntimeExtensions
{
    public static IServiceCollection AddIceBotClientDeviceRuntime(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<KioskSalesAdmissionOptions>()
            .Bind(configuration.GetSection(KioskSalesAdmissionOptions.SectionName));
        services.AddOptions<ClientDeviceRuntimeOptions>()
            .Bind(configuration.GetSection(ClientDeviceRuntimeOptions.SectionName))
            .Validate(options => options.MaxOrderLines is >= 1 and <= 100 &&
                                 options.MaxQuantityPerLine is >= 1 and <= 100 &&
                                 options.MaxTotalUnits >= options.MaxQuantityPerLine &&
                                 options.MaxSelectedOptionsPerLine is >= 0 and <= 100 &&
                                 options.MaxClientOrderIdLength is >= 1 and <= 200 &&
                                 options.MaxCustomerNameLength is >= 1 and <= 200 &&
                                 options.MaxCustomerPhoneNumberLength is >= 1 and <= 100 &&
                                 options.MaxNotesLength is >= 1 and <= 4_000 &&
                                 options.MaxClientLineIdLength is >= 1 and <= 200,
                "Client-device runtime order limits are invalid.")
            .ValidateOnStart();

        return services;
    }
}
