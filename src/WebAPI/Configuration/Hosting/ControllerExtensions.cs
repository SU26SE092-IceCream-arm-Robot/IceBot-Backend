using Application.Shared.Wrappers;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace WebAPI.Configuration.Hosting;

public static class ControllerExtensions
{
    public static IServiceCollection AddIceBotControllers(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.Converters.Add(
                new JsonStringEnumConverter(null, allowIntegerValues: false));
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var response = ApiResult<object>.Fail("Validation failed", 400);
                foreach (var item in context.ModelState)
                {
                    var firstError = item.Value?.Errors.FirstOrDefault();
                    if (firstError is not null)
                    {
                        response.AddValidationError(item.Key, firstError.ErrorMessage ?? "Invalid");
                    }
                }

                return new BadRequestObjectResult(response);
            };
        });

        return services;
    }
}
