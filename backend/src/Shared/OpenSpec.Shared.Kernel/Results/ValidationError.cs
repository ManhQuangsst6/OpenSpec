namespace OpenSpec.Shared.Kernel.Results;

/// <summary>Lỗi dữ liệu đầu vào gồm danh sách lỗi theo từng trường (key camelCase như trong request).</summary>
public sealed record ValidationError(string Code, string Message, IReadOnlyDictionary<string, string[]> Errors)
    : Error(Code, Message);
