using SpaceHub.Domain.Entities;
namespace SpaceHub.Application.Auth;

public interface ITokenService
{
    AccessToken Create(User user);
}
