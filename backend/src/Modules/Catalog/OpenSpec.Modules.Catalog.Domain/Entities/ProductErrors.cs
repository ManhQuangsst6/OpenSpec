using OpenSpec.Shared.Kernel.Results;

namespace OpenSpec.Modules.Catalog.Domain.Entities;

public static class ProductErrors
{
    public static readonly Error SkuTaken = new("Product.SkuTaken", "SKU đã được sử dụng.");
    public static readonly Error NotFound = new("Product.NotFound", "Không tìm thấy sản phẩm.");

    public static ValidationError Validation(IReadOnlyDictionary<string, string[]> errors) =>
        new("Product.Validation", "Dữ liệu sản phẩm không hợp lệ.", errors);

    public static ValidationError InvalidQuery(IReadOnlyDictionary<string, string[]> errors) =>
        new("Product.InvalidQuery", "Tham số truy vấn không hợp lệ.", errors);
}
