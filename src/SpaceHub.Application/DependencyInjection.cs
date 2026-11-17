using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SpaceHub.Application.Auth;
using SpaceHub.Application.Registrations;
using SpaceHub.Application.Events;
using SpaceHub.Application.Bookings;
using SpaceHub.Application.Catalog;
using SpaceHub.Application.Spaces;
using SpaceHub.Application.Mapping;
namespace SpaceHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<AuthService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddAutoMapper(cfg => { }, typeof(SpaceProfile));
        services.AddScoped<ISpaceService, SpaceHub.Application.Spaces.SpaceService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IBookingService, BookingAppService>();
        services.AddScoped<IEventService, EventAppService>();
        services.AddScoped<IRegistrationService, RegistrationAppService>();
        return services;
    }
}
