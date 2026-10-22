using OpenSpec.Modules.Users.Domain.Entities;
using OpenSpec.Shared.Kernel.Results;

namespace OpenSpec.Modules.Users.Application.Users;

public interface IUserRepository
{
    Task<User?> FindAsync(Guid id, CancellationToken ct);

    Task<bool> EmailExistsAsync(string email, Guid? exceptId, CancellationToken ct);

    /// <summary>Trang người dùng, mới nhất trước; <paramref name="search"/> khớp một phần email hoặc họ tên.</summary>
    Task<(IReadOnlyList<User> Items, int TotalCount)> ListAsync(int page, int pageSize, string? search, CancellationToken ct);

    void Add(User user);

    void Remove(User user);

    /// <summary>Lưu thay đổi; trả <see cref="UserErrors.EmailTaken"/> nếu vi phạm unique email.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
