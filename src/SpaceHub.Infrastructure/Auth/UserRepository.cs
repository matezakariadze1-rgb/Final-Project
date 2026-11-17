using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SpaceHub.Application.Auth;
using SpaceHub.Application.Exceptions;
using SpaceHub.Domain.Constants;
using SpaceHub.Domain.Entities;
using SpaceHub.Infrastructure.Data;

namespace SpaceHub.Infrastructure.Auth;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext db;

    // დამოკიდებულებები კონსტრუქტორით გადმოგვეცემა.
    public UserRepository(AppDbContext db)
    {
        this.db = db;
    }

    private IQueryable<User> WithRoles => db.UserRecords.Include(x => x.UserRoles).ThenInclude(x => x.Role);
    public Task<User?> FindByEmailAsync(string email, CancellationToken ct)
        => WithRoles.SingleOrDefaultAsync(x => x.Email == email, ct);
    public Task<User?> FindByIdAsync(int id, CancellationToken ct)
        => WithRoles.SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAttendeeAsync(User user, CancellationToken ct)
    {
        var role = await db.RoleRecords.SingleAsync(x => x.Name == RoleNames.Attendee, ct);
        user.UserRoles.Add(new UserRole { User = user, Role = role });
        db.UserRecords.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsDuplicate(ex))
        {
            throw new BusinessException(FailureKind.Conflict, "An account with this email already exists.");
        }
    }

    public async Task AddRoleAsync(User user, string roleName, CancellationToken ct)
    {
        var role = await db.RoleRecords.SingleAsync(x => x.Name == roleName, ct);
        var link = new UserRole { UserId = user.Id, RoleId = role.Id, User = user, Role = role };
        user.UserRoles.Add(link);
        db.UserRoleRecords.Add(link);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsDuplicate(ex))
        {
            // Concurrent activation is idempotent; the existing link already grants this role.
            db.Entry(link).State = EntityState.Detached;
            user.UserRoles.Remove(link);
            await db.Entry(user).Collection(x => x.UserRoles).Query().Include(x => x.Role).LoadAsync(ct);
        }
    }

    public async Task SaveAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
    private static bool IsDuplicate(DbUpdateException ex)
        => ex.InnerException is SqlException { Number: 2601 or 2627 };
}
