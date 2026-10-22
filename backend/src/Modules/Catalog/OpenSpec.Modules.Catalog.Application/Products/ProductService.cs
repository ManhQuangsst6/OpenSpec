using OpenSpec.Modules.Catalog.Domain.Entities;
using OpenSpec.Shared.Kernel.Results;

namespace OpenSpec.Modules.Catalog.Application.Products;

public sealed class ProductService(IProductRepository repository, TimeProvider timeProvider)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    private static readonly Dictionary<string, ProductSortField> SortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["createdAt"] = ProductSortField.CreatedAt,
        ["sku"] = ProductSortField.Sku,
        ["name"] = ProductSortField.Name,
        ["price"] = ProductSortField.Price,
        ["stock"] = ProductSortField.Stock,
    };

    /// <summary>Kiểm tra tham số thô từ query string và chuyển thành <see cref="ProductListQuery"/>.</summary>
    public static Result<ProductListQuery> ParseQuery(
        int page, int pageSize, string? search, bool? isActive, string? sortBy, string? sortDir)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1)
            errors["page"] = ["page phải ≥ 1."];
        if (pageSize is < 1 or > MaxPageSize)
            errors["pageSize"] = [$"pageSize phải trong khoảng 1..{MaxPageSize}."];

        var sortField = ProductSortField.CreatedAt;
        if (!string.IsNullOrWhiteSpace(sortBy) && !SortFields.TryGetValue(sortBy.Trim(), out sortField))
            errors["sortBy"] = [$"sortBy phải là một trong: {string.Join(", ", SortFields.Keys)}."];

        var direction = string.IsNullOrWhiteSpace(sortDir) ? null : sortDir.Trim().ToLowerInvariant();
        if (direction is not (null or "asc" or "desc"))
            errors["sortDir"] = ["sortDir phải là asc hoặc desc."];

        if (errors.Count > 0)
            return Result<ProductListQuery>.Failure(ProductErrors.InvalidQuery(errors));

        // Mặc định: mới nhất trước khi không chỉ định cột; các cột khác mặc định tăng dần
        var descending = direction is null ? string.IsNullOrWhiteSpace(sortBy) : direction == "desc";
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        return Result<ProductListQuery>.Success(new ProductListQuery(page, pageSize, term, isActive, sortField, descending));
    }

    public async Task<Result<PagedResult<ProductDto>>> ListAsync(ProductListQuery query, CancellationToken ct)
    {
        var (items, total) = await repository.ListAsync(query, ct);
        return Result<PagedResult<ProductDto>>.Success(
            new PagedResult<ProductDto>(items.Select(ProductDto.From).ToList(), query.Page, query.PageSize, total));
    }

    public async Task<Result<ProductDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var product = await repository.FindAsync(id, ct);
        return product is null
            ? Result<ProductDto>.Failure(ProductErrors.NotFound)
            : Result<ProductDto>.Success(ProductDto.From(product));
    }

    public async Task<Result<ProductDto>> CreateAsync(SaveProductRequest request, CancellationToken ct)
    {
        var created = Product.Create(request.ToData(), timeProvider.GetUtcNow());
        if (!created.IsSuccess)
            return Result<ProductDto>.Failure(created.Error!);

        var product = created.Value!;
        if (await repository.SkuExistsAsync(product.Sku, null, ct))
            return Result<ProductDto>.Failure(ProductErrors.SkuTaken);

        repository.Add(product);
        return await SaveAsync(product, ct);
    }

    public async Task<Result<ProductDto>> UpdateAsync(Guid id, SaveProductRequest request, CancellationToken ct)
    {
        var product = await repository.FindAsync(id, ct);
        if (product is null)
            return Result<ProductDto>.Failure(ProductErrors.NotFound);

        var updated = product.Update(request.ToData(), timeProvider.GetUtcNow());
        if (!updated.IsSuccess)
            return Result<ProductDto>.Failure(updated.Error!);

        if (await repository.SkuExistsAsync(product.Sku, id, ct))
            return Result<ProductDto>.Failure(ProductErrors.SkuTaken);

        return await SaveAsync(product, ct);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var product = await repository.FindAsync(id, ct);
        if (product is null)
            return Result.Failure(ProductErrors.NotFound);

        repository.Remove(product);
        return await repository.SaveChangesAsync(ct);
    }

    private async Task<Result<ProductDto>> SaveAsync(Product product, CancellationToken ct)
    {
        var saved = await repository.SaveChangesAsync(ct);
        return saved.IsSuccess ? Result<ProductDto>.Success(ProductDto.From(product)) : Result<ProductDto>.Failure(saved.Error!);
    }
}
