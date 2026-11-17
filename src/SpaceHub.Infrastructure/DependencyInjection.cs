using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SpaceHub.Infrastructure.Data;
using SpaceHub.Infrastructure.Auth;
using SpaceHub.Application.Auth;
using SpaceHub.Application.Registrations;
using SpaceHub.Infrastructure.Registrations;
using SpaceHub.Application.Events;
using SpaceHub.Infrastructure.Events;
using SpaceHub.Application.Bookings;
using SpaceHub.Infrastructure.Bookings;
using SpaceHub.Application.Catalog;
using SpaceHub.Infrastructure.Catalog;
using SpaceHub.Application.Spaces;
using SpaceHub.Infrastructure.Spaces;

namespace SpaceHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ISpaceRepository, SpaceRepository>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IRegistrationRepository, RegistrationRepository>();
        return services;
    }
}
