using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SpaceHub.Application.Auth;
using SpaceHub.Domain.Entities;
namespace SpaceHub.Infrastructure.Auth;

public sealed class TokenService : ITokenService
{
    private readonly IOptions<JwtSettings> settings;

    // დამოკიდებულებები კონსტრუქტორით გადმოგვეცემა.
    public TokenService(IOptions<JwtSettings> settings)
    {
        this.settings = settings;
    }

    public AccessToken Create(User user)
    {
        var options = settings.Value;
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(options.LifetimeMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        claims.AddRange(user.UserRoles.Select(x => new Claim("role", x.Role.Name)));
        var token = new JwtSecurityToken(options.Issuer, options.Audience, claims, now, expires,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)), SecurityAlgorithms.HmacSha256));
        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
