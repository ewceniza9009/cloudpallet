using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using FluentValidation;
using MediatR;
using WMS.Application.Common.Behaviors;
using WMS.Domain.Services;
using WMS.Application.Services;

namespace WMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddScoped<IPutawaySuggestionService, PutawaySuggestionService>();

        // Register all VAS Service Handlers
        var handlerType = typeof(Features.Inventory.Services.IVasServiceHandler);
        var handlers = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => handlerType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var handler in handlers)
        {
            services.AddScoped(handlerType, handler);
        }

        return services;
    }
}