using OpenSpec.Shared.Kernel.Domain;
using OpenSpec.Shared.Kernel.Results;

namespace OpenSpec.Modules.Users.Domain.Entities;

public sealed class User : Entity<Guid>
{
    private User() { }

    public string Email { get; private set; } = string.Empty;

    public static Result<User> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result<User>.Failure(new Error("User.InvalidEmail", "Email không hợp lệ."));

        return Result<User>.Success(new User { Id = Guid.NewGuid(), Email = email.Trim().ToLowerInvariant() });
    }
}
