namespace SpaceHub.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<UserResponse> GetUserAsync(int userId, CancellationToken ct);
    Task<AuthResponse> ActivateRoleAsync(int userId, string role, CancellationToken ct);
}
