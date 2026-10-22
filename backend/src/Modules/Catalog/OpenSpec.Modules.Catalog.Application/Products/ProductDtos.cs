using OpenSpec.Modules.Catalog.Domain.Entities;

namespace OpenSpec.Modules.Catalog.Application.Products;

public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static ProductDto From(Product p) =>
        new(p.Id, p.Sku, p.Name, p.Description, p.Price, p.Stock, p.IsActive, p.CreatedAt, p.UpdatedAt);
}

/// <summary>Dữ liệu tạo/cập nhật sản phẩm (dùng chung cho POST và PUT).</summary>
public sealed record SaveProductRequest(string Sku, string Name, string? Description, decimal Price, int Stock, bool IsActive)
{
    public ProductData ToData() => new(Sku, Name, Description, Price, Stock, IsActive);
}

public enum ProductSortField
{
    CreatedAt,
    Sku,
    Name,
    Price,
    Stock,
}

/// <summary>Truy vấn danh sách đã được kiểm tra hợp lệ.</summary>
public sealed record ProductListQuery(int Page, int PageSize, string? Search, bool? IsActive, ProductSortField SortBy, bool Descending);
