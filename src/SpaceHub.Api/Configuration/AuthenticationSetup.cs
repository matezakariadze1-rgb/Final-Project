using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SpaceHub.Api.DTOs;
using SpaceHub.Infrastructure.Auth;
namespace SpaceHub.Api.Configuration;

public static class AuthenticationSetup
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Jwt");
        var settings = section.Get<JwtSettings>() ?? throw new InvalidOperationException("Jwt configuration is missing.");
        if (string.IsNullOrWhiteSpace(settings.Key) || Encoding.UTF8.GetByteCount(settings.Key) < 32
            || string.IsNullOrWhiteSpace(settings.Issuer) || string.IsNullOrWhiteSpace(settings.Audience)
            || settings.LifetimeMinutes is < 60 or > 1440)
            throw new InvalidOperationException("Configure Jwt:Key (at least 32 bytes), Issuer, Audience and LifetimeMinutes (60-1440).");
        services.Configure<JwtSettings>(section);
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = settings.Issuer,
                ValidateAudience = true,
                ValidAudience = settings.Audience,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                NameClaimType = "sub",
                RoleClaimType = "role",
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = 401;
                    context.Response.Headers.WWWAuthenticate = "Bearer";
                    await context.Response.WriteAsJsonAsync(ErrorResponse.Create(401, "Authentication required."));
                },
                OnForbidden = context => context.Response.WriteAsJsonAsync(ErrorResponse.Create(403, "Access denied."))
            };
        });
        services.AddAuthorization();
        return services;
    }
}
