using System.Text.RegularExpressions;
using OpenSpec.Shared.Kernel.Domain;
using OpenSpec.Shared.Kernel.Results;

namespace OpenSpec.Modules.Catalog.Domain.Entities;

public sealed partial class Product : Entity<Guid>
{
    public const int SkuMinLength = 3;
    public const int SkuMaxLength = 32;
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const decimal MaxPrice = 1_000_000_000m;

    private Product() { }

    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<Product> Create(ProductData data, DateTimeOffset now)
    {
        var product = new Product { Id = Guid.NewGuid(), CreatedAt = now };
        var result = product.Update(data, now);
        return result.IsSuccess ? Result<Product>.Success(product) : Result<Product>.Failure(result.Error!);
    }

    /// <summary>Kiểm tra toàn bộ trường cùng lúc; chỉ thay đổi sản phẩm khi mọi trường hợp lệ.</summary>
    public Result Update(ProductData data, DateTimeOffset now)
    {
        var errors = new Dictionary<string, string[]>();

        var sku = NormalizeSku(data.Sku);
        if (sku is null)
            errors["sku"] = [$"SKU gồm {SkuMinLength}–{SkuMaxLength} ký tự chữ, số hoặc dấu gạch ngang."];

        var name = data.Name?.Trim() ?? string.Empty;
        if (name.Length == 0 || name.Length > NameMaxLength)
            errors["name"] = [$"Tên bắt buộc và tối đa {NameMaxLength} ký tự."];

        var description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim();
        if (description?.Length > DescriptionMaxLength)
            errors["description"] = [$"Mô tả tối đa {DescriptionMaxLength} ký tự."];

        if (data.Price < 0 || data.Price > MaxPrice || decimal.Round(data.Price, 2) != data.Price)
            errors["price"] = ["Giá phải từ 0 đến 1.000.000.000 và tối đa 2 chữ số thập phân."];

        if (data.Stock < 0)
            errors["stock"] = ["Tồn kho không được âm."];

        if (errors.Count > 0)
            return Result.Failure(ProductErrors.Validation(errors));

        Sku = sku!;
        Name = name;
        Description = description;
        Price = data.Price;
        Stock = data.Stock;
        IsActive = data.IsActive;
        UpdatedAt = now;
        return Result.Success();
    }

    /// <summary>Trả SKU đã chuẩn hoá (trim, chữ hoa) hoặc null nếu không hợp lệ.</summary>
    public static string? NormalizeSku(string? sku)
    {
        var value = sku?.Trim().ToUpperInvariant() ?? string.Empty;
        return value.Length is >= SkuMinLength and <= SkuMaxLength && SkuPattern().IsMatch(value) ? value : null;
    }

    [GeneratedRegex("^[A-Z0-9-]+$")]
    private static partial Regex SkuPattern();
}

public sealed record ProductData(string Sku, string Name, string? Description, decimal Price, int Stock, bool IsActive);
