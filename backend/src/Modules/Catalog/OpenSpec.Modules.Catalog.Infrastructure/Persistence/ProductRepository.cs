using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OpenSpec.Modules.Catalog.Application.Products;
using OpenSpec.Modules.Catalog.Domain.Entities;
using OpenSpec.Shared.Kernel.Results;

namespace OpenSpec.Modules.Catalog.Infrastructure.Persistence;

internal sealed class ProductRepository(CatalogDbContext db) : IProductRepository
{
    // Lỗi SQL Server khi vi phạm unique index / unique constraint
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public Task<Product?> FindAsync(Guid id, CancellationToken ct) =>
        db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> SkuExistsAsync(string sku, Guid? exceptId, CancellationToken ct) =>
        db.Products.AnyAsync(p => p.Sku == sku && p.Id != exceptId, ct);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> ListAsync(ProductListQuery query, CancellationToken ct)
    {
        var products = db.Products.AsNoTracking();

        if (query.Search is not null)
        {
            var pattern = $"%{EscapeLike(query.Search)}%";
            products = products.Where(p => EF.Functions.Like(p.Sku, pattern, @"\") || EF.Functions.Like(p.Name, pattern, @"\"));
        }

        if (query.IsActive is { } isActive)
            products = products.Where(p => p.IsActive == isActive);

        var total = await products.CountAsync(ct);
        var items = await Sort(products, query.SortBy, query.Descending)
            .ThenBy(p => p.Id) // thứ tự ổn định giữa các trang khi giá trị cột trùng nhau
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public void Add(Product product) => db.Products.Add(product);

    public void Remove(Product product) => db.Products.Remove(product);

    public async Task<Result> SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation })
        {
            db.ChangeTracker.Clear();
            return Result.Failure(ProductErrors.SkuTaken);
        }
    }

    private static IOrderedQueryable<Product> Sort(IQueryable<Product> products, ProductSortField field, bool descending) =>
        field switch
        {
            ProductSortField.Sku => descending ? products.OrderByDescending(p => p.Sku) : products.OrderBy(p => p.Sku),
            ProductSortField.Name => descending ? products.OrderByDescending(p => p.Name) : products.OrderBy(p => p.Name),
            ProductSortField.Price => descending ? products.OrderByDescending(p => p.Price) : products.OrderBy(p => p.Price),
            ProductSortField.Stock => descending ? products.OrderByDescending(p => p.Stock) : products.OrderBy(p => p.Stock),
            _ => descending ? products.OrderByDescending(p => p.CreatedAt) : products.OrderBy(p => p.CreatedAt),
        };

    private static string EscapeLike(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_").Replace("[", @"\[");
}
