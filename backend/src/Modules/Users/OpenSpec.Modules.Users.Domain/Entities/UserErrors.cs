using OpenSpec.Shared.Kernel.Results;

namespace OpenSpec.Modules.Users.Domain.Entities;

public static class UserErrors
{
    public static readonly Error InvalidEmail = new("User.InvalidEmail", "Email không hợp lệ.");
    public static readonly Error InvalidFullName = new("User.InvalidFullName", $"Họ tên bắt buộc và tối đa {User.FullNameMaxLength} ký tự.");
    public static readonly Error EmailTaken = new("User.EmailTaken", "Email đã được sử dụng.");
    public static readonly Error NotFound = new("User.NotFound", "Không tìm thấy người dùng.");
}
