using System.Net.Mail;
using OpenSpec.Shared.Kernel.Domain;
using OpenSpec.Shared.Kernel.Results;

namespace OpenSpec.Modules.Users.Domain.Entities;

public sealed class User : Entity<Guid>
{
    public const int EmailMaxLength = 256;
    public const int FullNameMaxLength = 200;

    private User() { }

    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private init; }

    public static Result<User> Create(string email, string fullName, DateTimeOffset createdAt)
    {
        var user = new User { Id = Guid.NewGuid(), CreatedAt = createdAt };
        var result = user.Update(email, fullName);
        return result.IsSuccess ? Result<User>.Success(user) : Result<User>.Failure(result.Error!);
    }

    public Result Update(string email, string fullName)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (normalizedEmail is null)
            return Result.Failure(UserErrors.InvalidEmail);

        var trimmedName = fullName?.Trim() ?? string.Empty;
        if (trimmedName.Length == 0 || trimmedName.Length > FullNameMaxLength)
            return Result.Failure(UserErrors.InvalidFullName);

        Email = normalizedEmail;
        FullName = trimmedName;
        return Result.Success();
    }

    /// <summary>Trả email đã chuẩn hoá (trim, chữ thường) hoặc null nếu không hợp lệ.</summary>
    public static string? NormalizeEmail(string? email)
    {
        var value = email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (value.Length == 0 || value.Length > EmailMaxLength)
            return null;

        return MailAddress.TryCreate(value, out var address) && address.Address == value ? value : null;
    }
}
