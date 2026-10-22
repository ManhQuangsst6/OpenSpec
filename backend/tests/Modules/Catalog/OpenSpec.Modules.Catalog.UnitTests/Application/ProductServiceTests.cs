using OpenSpec.Modules.Catalog.Application.Products;
using OpenSpec.Modules.Catalog.Domain.Entities;
using OpenSpec.Shared.Kernel.Results;

namespace OpenSpec.Modules.Catalog.UnitTests.Application;

public class ProductServiceTests
{
    private readonly FakeProductRepository _repository = new();
    private readonly ProductService _service;

    public ProductServiceTests() => _service = new ProductService(_repository, TimeProvider.System);

    private static SaveProductRequest Request(string sku, string name = "Sản phẩm") => new(sku, name, null, 10_000m, 5, true);

    [Fact]
    public async Task Create_WithNewSku_AddsProduct()
    {
        var result = await _service.CreateAsync(Request("ao-01"), default);

        Assert.Equal("AO-01", result.Value!.Sku);
        Assert.Single(_repository.Products);
    }

    [Fact]
    public async Task Create_WithExistingSku_ReturnsSkuTaken()
    {
        await _service.CreateAsync(Request("AO-01"), default);

        var result = await _service.CreateAsync(Request("ao-01"), default);

        Assert.Equal("Product.SkuTaken", result.Error!.Code);
        Assert.Single(_repository.Products);
    }

    [Fact]
    public async Task Create_WithInvalidData_ReturnsValidationError()
    {
        var result = await _service.CreateAsync(Request("x", ""), default);

        var error = Assert.IsType<ValidationError>(result.Error);
        Assert.Contains("sku", error.Errors.Keys);
        Assert.Contains("name", error.Errors.Keys);
    }

    [Fact]
    public async Task Update_ToSkuOfAnotherProduct_ReturnsSkuTaken()
    {
        await _service.CreateAsync(Request("A-01"), default);
        var b = (await _service.CreateAsync(Request("B-01", "B"), default)).Value!;

        var result = await _service.UpdateAsync(b.Id, Request("a-01", "B2"), default);

        Assert.Equal("Product.SkuTaken", result.Error!.Code);
    }

    [Fact]
    public async Task Update_KeepingOwnSku_Succeeds()
    {
        var a = (await _service.CreateAsync(Request("A-01"), default)).Value!;

        var result = await _service.UpdateAsync(a.Id, Request("a-01", "Mới"), default);

        Assert.Equal("Mới", result.Value!.Name);
    }

    [Fact]
    public async Task UnknownId_ReturnsNotFound()
    {
        var id = Guid.NewGuid();

        Assert.Equal("Product.NotFound", (await _service.GetAsync(id, default)).Error!.Code);
        Assert.Equal("Product.NotFound", (await _service.UpdateAsync(id, Request("A-01"), default)).Error!.Code);
        Assert.Equal("Product.NotFound", (await _service.DeleteAsync(id, default)).Error!.Code);
    }

    [Theory]
    [InlineData(0, 20, null, null, "page")]
    [InlineData(1, 101, null, null, "pageSize")]
    [InlineData(1, 20, "password", null, "sortBy")]
    [InlineData(1, 20, "price", "up", "sortDir")]
    public void ParseQuery_WithInvalidInput_ReturnsInvalidQuery(int page, int size, string? sortBy, string? sortDir, string field)
    {
        var result = ProductService.ParseQuery(page, size, null, null, sortBy, sortDir);

        var error = Assert.IsType<ValidationError>(result.Error);
        Assert.Equal("Product.InvalidQuery", error.Code);
        Assert.Equal([field], error.Errors.Keys);
    }

    [Theory]
    [InlineData(null, null, ProductSortField.CreatedAt, true)]
    [InlineData("price", null, ProductSortField.Price, false)]
    [InlineData("PRICE", "desc", ProductSortField.Price, true)]
    [InlineData("name", "asc", ProductSortField.Name, false)]
    public void ParseQuery_ResolvesSortDefaults(string? sortBy, string? sortDir, ProductSortField field, bool descending)
    {
        var query = ProductService.ParseQuery(1, 20, "  ", true, sortBy, sortDir).Value!;

        Assert.Equal((field, descending), (query.SortBy, query.Descending));
        Assert.Null(query.Search);
        Assert.True(query.IsActive);
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public List<Product> Products { get; } = [];

        public Task<Product?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult(Products.FirstOrDefault(p => p.Id == id));

        public Task<bool> SkuExistsAsync(string sku, Guid? exceptId, CancellationToken ct) =>
            Task.FromResult(Products.Any(p => p.Sku == sku && p.Id != exceptId));

        public Task<(IReadOnlyList<Product> Items, int TotalCount)> ListAsync(ProductListQuery query, CancellationToken ct) =>
            Task.FromResult(((IReadOnlyList<Product>)Products, Products.Count));

        public void Add(Product product) => Products.Add(product);

        public void Remove(Product product) => Products.Remove(product);

        public Task<Result> SaveChangesAsync(CancellationToken ct) => Task.FromResult(Result.Success());
    }
}
