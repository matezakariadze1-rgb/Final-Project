using FluentValidation;
using SpaceHub.Application.Exceptions;
using SpaceHub.Domain.Constants;
using SpaceHub.Domain.Entities;

namespace SpaceHub.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository users;
    private readonly IPasswordService passwords;
    private readonly ITokenService tokens;
    private readonly IValidator<RegisterRequest> registerValidator;
    private readonly IValidator<LoginRequest> loginValidator;

    // დამოკიდებულებები კონსტრუქტორით გადმოგვეცემა.
    public AuthService(IUserRepository users, IPasswordService passwords, ITokenService tokens,
    IValidator<RegisterRequest> registerValidator, IValidator<LoginRequest> loginValidator)
    {
        this.users = users;
        this.passwords = passwords;
        this.tokens = tokens;
        this.registerValidator = registerValidator;
        this.loginValidator = loginValidator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        await registerValidator.ValidateAndThrowAsync(request, ct);
        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.FindByEmailAsync(email, ct) is not null)
            throw new BusinessException(FailureKind.Conflict, "An account with this email already exists.");
        var user = new User { Email = email, FullName = request.FullName.Trim(), CreatedAt = DateTime.UtcNow };
        user.PasswordHash = passwords.Hash(user, request.Password);
        await users.AddAttendeeAsync(user, ct);
        return CreateResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        await loginValidator.ValidateAndThrowAsync(request, ct);
        var user = await users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (user is null || !passwords.Verify(user, request.Password, out var needsRehash))
            throw new BusinessException(FailureKind.Unauthorized, "Invalid email or password.");
        if (needsRehash)
        {
            user.PasswordHash = passwords.Hash(user, request.Password);
            await users.SaveAsync(ct);
        }
        return CreateResponse(user);
    }

    public async Task<UserResponse> GetUserAsync(int userId, CancellationToken ct)
        => MapUser(await RequireUserAsync(userId, ct));

    public async Task<AuthResponse> ActivateRoleAsync(int userId, string role, CancellationToken ct)
    {
        // These are self-service business capabilities, never administrative privileges.
        string roleName;
        string requestedRole = role.ToLowerInvariant();
        if (requestedRole == "owner")
        {
            roleName = RoleNames.Owner;
        }
        else if (requestedRole == "organizer")
        {
            roleName = RoleNames.Organizer;
        }
        else
        {
            throw new BusinessException(FailureKind.Validation, "Only Owner and Organizer roles can be activated.");
        }
        var user = await RequireUserAsync(userId, ct);
        if (!user.UserRoles.Any(x => x.Role.Name == roleName))
            await users.AddRoleAsync(user, roleName, ct);
        return CreateResponse(user);
    }

    private async Task<User> RequireUserAsync(int id, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id, ct);
        if (user == null)
        {
            throw new BusinessException(FailureKind.Unauthorized, "Account is not available.");
        }
        return user;
    }

    private AuthResponse CreateResponse(User user)
    {
        var token = tokens.Create(user);
        return new AuthResponse(token.Value, token.ExpiresAt, MapUser(user));
    }

    private static UserResponse MapUser(User user) => new(user.Id, user.Email, user.FullName,
        user.UserRoles.Select(x => x.Role.Name).Order().ToArray());
}
