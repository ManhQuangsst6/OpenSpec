using OpenSpec.Modules.Users.Domain.Entities;
using OpenSpec.Shared.Kernel.Results;

namespace OpenSpec.Modules.Users.Application.Users;

public sealed class UserService(IUserRepository repository, TimeProvider timeProvider)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static readonly Error InvalidPaging =
        new("User.InvalidPaging", $"page phải ≥ 1 và pageSize trong khoảng 1..{MaxPageSize}.");

    public async Task<Result<PagedResult<UserDto>>> ListAsync(int page, int pageSize, string? search, CancellationToken ct)
    {
        if (page < 1 || pageSize < 1 || pageSize > MaxPageSize)
            return Result<PagedResult<UserDto>>.Failure(InvalidPaging);

        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var (items, total) = await repository.ListAsync(page, pageSize, term, ct);

        return Result<PagedResult<UserDto>>.Success(
            new PagedResult<UserDto>(items.Select(UserDto.From).ToList(), page, pageSize, total));
    }

    public async Task<Result<UserDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var user = await repository.FindAsync(id, ct);
        return user is null ? Result<UserDto>.Failure(UserErrors.NotFound) : Result<UserDto>.Success(UserDto.From(user));
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        var created = User.Create(request.Email, request.FullName, timeProvider.GetUtcNow());
        if (!created.IsSuccess)
            return Result<UserDto>.Failure(created.Error!);

        var user = created.Value!;
        if (await repository.EmailExistsAsync(user.Email, null, ct))
            return Result<UserDto>.Failure(UserErrors.EmailTaken);

        repository.Add(user);
        var saved = await repository.SaveChangesAsync(ct);
        return saved.IsSuccess ? Result<UserDto>.Success(UserDto.From(user)) : Result<UserDto>.Failure(saved.Error!);
    }

    public async Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct)
    {
        var user = await repository.FindAsync(id, ct);
        if (user is null)
            return Result<UserDto>.Failure(UserErrors.NotFound);

        var normalizedEmail = User.NormalizeEmail(request.Email);
        if (normalizedEmail is not null && await repository.EmailExistsAsync(normalizedEmail, id, ct))
            return Result<UserDto>.Failure(UserErrors.EmailTaken);

        var updated = user.Update(request.Email, request.FullName);
        if (!updated.IsSuccess)
            return Result<UserDto>.Failure(updated.Error!);

        var saved = await repository.SaveChangesAsync(ct);
        return saved.IsSuccess ? Result<UserDto>.Success(UserDto.From(user)) : Result<UserDto>.Failure(saved.Error!);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var user = await repository.FindAsync(id, ct);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        repository.Remove(user);
        return await repository.SaveChangesAsync(ct);
    }
}
