using Microsoft.AspNetCore.Identity;
using SpaceHub.Application.Auth;
using SpaceHub.Domain.Entities;
namespace SpaceHub.Infrastructure.Auth;

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<User> hasher = new();
    public string Hash(User user, string password) => hasher.HashPassword(user, password);
    public bool Verify(User user, string password, out bool needsRehash)
    {
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        needsRehash = result == PasswordVerificationResult.SuccessRehashNeeded;
        return result != PasswordVerificationResult.Failed;
    }
}
