using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SpaceHub.Application.Bookings;
using SpaceHub.Application.Exceptions;
using SpaceHub.Domain.Entities;
using SpaceHub.Infrastructure.Data;
namespace SpaceHub.Infrastructure.Bookings;

public sealed class SpaceBookingTransaction : ISpaceBookingTransaction
{
    private readonly AppDbContext db;
    private readonly IDbContextTransaction transaction;
    private readonly Space space;

    // დამოკიდებულებები კონსტრუქტორით გადმოგვეცემა.
    public SpaceBookingTransaction(AppDbContext db, IDbContextTransaction transaction, Space space)
    {
        this.db = db;
        this.transaction = transaction;
        this.space = space;
        Space = space;
    }

    public Space Space
    {
        get;
    }
    public async Task CommitAsync(CancellationToken ct)
    {
        db.Entry(Space).Property(x => x.IsActive).IsModified = true;
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessException(FailureKind.Conflict, "Concurrent update detected. Reload and retry.");
        }
    }
    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
