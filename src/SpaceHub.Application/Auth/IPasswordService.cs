using SpaceHub.Domain.Entities;
namespace SpaceHub.Application.Auth;

public interface IPasswordService
{
    string Hash(User user, string password);
    bool Verify(User user, string password, out bool needsRehash);
}
