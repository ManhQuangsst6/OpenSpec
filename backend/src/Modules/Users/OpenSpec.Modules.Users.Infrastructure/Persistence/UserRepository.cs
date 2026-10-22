using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OpenSpec.Modules.Users.Application.Users;
using OpenSpec.Modules.Users.Domain.Entities;
using OpenSpec.Shared.Kernel.Results;

namespace OpenSpec.Modules.Users.Infrastructure.Persistence;

internal sealed class UserRepository(UsersDbContext db) : IUserRepository
{
    // Lỗi SQL Server khi vi phạm unique index / unique constraint
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public Task<User?> FindAsync(Guid id, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<bool> EmailExistsAsync(string email, Guid? exceptId, CancellationToken ct) =>
        db.Users.AnyAsync(u => u.Email == email && u.Id != exceptId, ct);

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> ListAsync(
        int page, int pageSize, string? search, CancellationToken ct)
    {
        var query = db.Users.AsNoTracking();

        if (search is not null)
        {
            var pattern = $"%{EscapeLike(search)}%";
            query = query.Where(u => EF.Functions.Like(u.Email, pattern, @"\") || EF.Functions.Like(u.FullName, pattern, @"\"));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .ThenBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public void Add(User user) => db.Users.Add(user);

    public void Remove(User user) => db.Users.Remove(user);

    public async Task<Result> SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation })
        {
            db.ChangeTracker.Clear();
            return Result.Failure(UserErrors.EmailTaken);
        }
    }

    private static string EscapeLike(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_").Replace("[", @"\[");
}
