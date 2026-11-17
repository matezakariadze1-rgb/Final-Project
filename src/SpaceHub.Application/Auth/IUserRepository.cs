using SpaceHub.Domain.Entities;
namespace SpaceHub.Application.Auth;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct);
    Task<User?> FindByIdAsync(int id, CancellationToken ct);
    Task AddAttendeeAsync(User user, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
    Task AddRoleAsync(User user, string role, CancellationToken ct);
}
