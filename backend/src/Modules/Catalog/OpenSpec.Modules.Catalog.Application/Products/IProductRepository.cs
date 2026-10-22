using OpenSpec.Modules.Catalog.Domain.Entities;
using OpenSpec.Shared.Kernel.Results;

namespace OpenSpec.Modules.Catalog.Application.Products;

public interface IProductRepository
{
    Task<Product?> FindAsync(Guid id, CancellationToken ct);

    Task<bool> SkuExistsAsync(string sku, Guid? exceptId, CancellationToken ct);

    Task<(IReadOnlyList<Product> Items, int TotalCount)> ListAsync(ProductListQuery query, CancellationToken ct);

    void Add(Product product);

    void Remove(Product product);

    /// <summary>Lưu thay đổi; trả <see cref="ProductErrors.SkuTaken"/> nếu vi phạm unique SKU.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
