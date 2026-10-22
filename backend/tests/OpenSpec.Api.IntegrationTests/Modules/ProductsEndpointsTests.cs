using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using OpenSpec.Api.IntegrationTests.Fixtures;

namespace OpenSpec.Api.IntegrationTests.Modules;

public class ProductsEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string BaseUrl = "/api/catalog/products";
    private readonly HttpClient _client = factory.CreateClient();

    private sealed record ProductResponse(
        Guid Id, string Sku, string Name, string? Description, decimal Price, int Stock, bool IsActive,
        DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

    private sealed record PageResponse(List<ProductResponse> Items, int Page, int PageSize, int TotalCount);

    // SKU riêng cho mỗi test để chạy độc lập trên cùng một DB
    private static string UniqueSku(string prefix = "P") => $"{prefix}-{Guid.NewGuid():N}"[..20].ToUpperInvariant();

    private static object Body(string sku, string name = "Sản phẩm", decimal price = 10_000m, int stock = 5, bool isActive = true) =>
        new { sku, name, description = (string?)null, price, stock, isActive };

    private async Task<ProductResponse> CreateAsync(object body)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }

    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task Create_ReturnsCreatedWithLocationAndNormalizedSku()
    {
        var sku = UniqueSku("ao");

        var response = await _client.PostAsJsonAsync(BaseUrl, Body($"  {sku.ToLowerInvariant()} ", "Áo thun", 150_000m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
        Assert.Equal((sku, "Áo thun", 150_000m), (product.Sku, product.Name, product.Price));
        Assert.Equal($"{BaseUrl}/{product.Id}", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Create_WithManyInvalidFields_ReturnsAllFieldErrors()
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { sku = "x", name = "", price = -1m, stock = -1, isActive = true });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ProblemAsync(response)).GetProperty("errors");
        Assert.Equal(["name", "price", "sku", "stock"], errors.EnumerateObject().Select(p => p.Name).Order());
    }

    [Fact]
    public async Task Create_WithMalformedBody_ReturnsBadRequest()
    {
        var content = new StringContent("{\"sku\":", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(BaseUrl, content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithDuplicateSku_ReturnsConflict()
    {
        var sku = UniqueSku();
        await CreateAsync(Body(sku));

        var response = await _client.PostAsJsonAsync(BaseUrl, Body(sku.ToLowerInvariant()));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Product.SkuTaken", (await ProblemAsync(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetUpdateDelete_FullLifecycle()
    {
        var product = await CreateAsync(Body(UniqueSku()));

        Assert.Equal(product, await _client.GetFromJsonAsync<ProductResponse>($"{BaseUrl}/{product.Id}"));

        var update = await _client.PutAsJsonAsync($"{BaseUrl}/{product.Id}", Body(product.Sku, "Tên mới", 99_000m, 0, false));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = (await update.Content.ReadFromJsonAsync<ProductResponse>())!;
        Assert.Equal(("Tên mới", 99_000m, 0, false), (updated.Name, updated.Price, updated.Stock, updated.IsActive));
        Assert.Equal(product.CreatedAt, updated.CreatedAt);
        Assert.True(updated.UpdatedAt > product.UpdatedAt);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"{BaseUrl}/{product.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"{BaseUrl}/{product.Id}")).StatusCode);

        // SKU đã xoá được phép dùng lại
        await CreateAsync(Body(product.Sku));
    }

    [Fact]
    public async Task Update_ToSkuOfAnotherProduct_ReturnsConflictAndKeepsData()
    {
        var a = await CreateAsync(Body(UniqueSku("A")));
        var b = await CreateAsync(Body(UniqueSku("B"), "B"));

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{b.Id}", Body(a.Sku, "B2"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(b, await _client.GetFromJsonAsync<ProductResponse>($"{BaseUrl}/{b.Id}"));
    }

    [Fact]
    public async Task UnknownId_ReturnsNotFound()
    {
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"{BaseUrl}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PutAsJsonAsync($"{BaseUrl}/{id}", Body(UniqueSku()))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"{BaseUrl}/{id}")).StatusCode);
    }

    [Fact]
    public async Task List_SearchesFiltersAndPaginates()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        for (var i = 0; i < 25; i++)
            await CreateAsync(Body(UniqueSku(), $"Áo {tag.ToUpperInvariant()} {i:00}", isActive: i % 5 != 0));

        var page2 = await _client.GetFromJsonAsync<PageResponse>($"{BaseUrl}?search={tag}&page=2&pageSize=20");
        Assert.Equal((25, 5), (page2!.TotalCount, page2.Items.Count));

        var inactive = await _client.GetFromJsonAsync<PageResponse>($"{BaseUrl}?search={tag}&isActive=false");
        Assert.Equal(5, inactive!.TotalCount);
        Assert.All(inactive.Items, p => Assert.False(p.IsActive));
    }

    [Fact]
    public async Task List_SortsByPriceWithStableOrderAcrossPages()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        decimal[] prices = [300, 100, 200, 100, 100, 50];
        foreach (var price in prices)
            await CreateAsync(Body(UniqueSku(), $"Giá {tag}", price));

        var url = $"{BaseUrl}?search={tag}&sortBy=price&sortDir=asc&pageSize=2";
        var all = new List<ProductResponse>();
        for (var page = 1; page <= 3; page++)
            all.AddRange((await _client.GetFromJsonAsync<PageResponse>($"{url}&page={page}"))!.Items);

        Assert.Equal(prices.Order(), all.Select(p => p.Price));
        Assert.Equal(6, all.Select(p => p.Id).Distinct().Count()); // không trùng/mất bản ghi giữa các trang

        var desc = await _client.GetFromJsonAsync<PageResponse>($"{BaseUrl}?search={tag}&sortBy=price&sortDir=desc");
        Assert.Equal(300m, desc!.Items[0].Price);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    [InlineData("sortBy=password")]
    [InlineData("sortDir=up")]
    public async Task List_WithInvalidQuery_ReturnsBadRequest(string query)
    {
        var response = await _client.GetAsync($"{BaseUrl}?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
