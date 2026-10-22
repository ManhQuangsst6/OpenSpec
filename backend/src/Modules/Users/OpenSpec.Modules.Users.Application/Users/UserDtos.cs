using OpenSpec.Modules.Users.Domain.Entities;

namespace OpenSpec.Modules.Users.Application.Users;

public sealed record UserDto(Guid Id, string Email, string FullName, DateTimeOffset CreatedAt)
{
    public static UserDto From(User user) => new(user.Id, user.Email, user.FullName, user.CreatedAt);
}

public sealed record CreateUserRequest(string Email, string FullName);

public sealed record UpdateUserRequest(string Email, string FullName);
